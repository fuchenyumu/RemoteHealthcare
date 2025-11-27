import json
import logging
from datetime import datetime

from fastapi import WebSocket, WebSocketDisconnect

from app.models import (
    JoinRoomPayload,
    Participant,
    ParticipantRole,
    Room,
    SignalMessage,
    SignalMessageType,
)
from app.services.room_manager import room_manager
from app.services.socket_manager import connection_manager
from app.services.token_service import token_service

logger = logging.getLogger(__name__)


async def medical_signaling_endpoint(websocket: WebSocket, room_id: str) -> None:
    """
    处理医疗会诊房间的信令连接。

    TODO: 集成 aiortc SFU 传递 offer/answer/candidate。
    """

    await websocket.accept()
    participant: Participant | None = None

    try:
        join_message = await websocket.receive_text()
        payload = JoinRoomPayload(**json.loads(join_message))
        token_claims = token_service.verify_token(payload.token)

        participant = Participant(
            user_id=str(token_claims["sub"]),
            display_name=token_claims.get("name", "未知用户"),
            role=ParticipantRole(token_claims.get("role", ParticipantRole.EXPERT)),
        )

        room = await room_manager.get_room(room_id)
        if not room:
            room = Room(
                room_id=room_id,
                consultation_id=int(token_claims["consultation_id"]),
                host_id=str(token_claims.get("host_id", participant.user_id)),
            )
            await room_manager.create_room(room)

        await room_manager.add_participant(room_id, participant)
        await connection_manager.connect(room_id, participant.user_id, websocket)

        await websocket.send_json(
            {
                "type": "JOINED",
                "participantId": participant.user_id,
                "roomId": room_id,
                "participants": [
                    serialize_participant(p)
                    for p in (await room_manager.list_participants(room_id)).values()
                ],
            }
        )

        await connection_manager.broadcast(
            room_id,
            {
                "type": "PARTICIPANT_JOINED",
                "roomId": room_id,
                "participant": serialize_participant(participant),
            },
            exclude=participant.user_id,
        )

        while True:
            message = await websocket.receive_text()
            await handle_signaling_message(room_id, participant.user_id, message)

    except WebSocketDisconnect:
        logger.info("WebSocket disconnected for room %s", room_id)
    except Exception as exc:  # pylint: disable=broad-except
        logger.exception("Signaling error: %s", exc)
        await websocket.close(code=4000)
    finally:
        if participant:
            await room_manager.remove_participant(room_id, participant.user_id)
            await connection_manager.disconnect(room_id, participant.user_id)
            await connection_manager.broadcast(
                room_id,
                {
                    "type": "PARTICIPANT_LEFT",
                    "roomId": room_id,
                    "participantId": participant.user_id,
                },
            )


async def handle_signaling_message(room_id: str, user_id: str, message: str) -> None:
    """
    预处理客户端发送的信令消息，转发给目标参与者或广播。
    """

    try:
        signal = SignalMessage.model_validate_json(message)
    except ValueError:
        logger.warning("Invalid signal message from %s: %s", user_id, message)
        return

    if signal.type in {
        SignalMessageType.OFFER,
        SignalMessageType.ANSWER,
        SignalMessageType.CANDIDATE,
    }:
        if not signal.target_id:
            logger.warning("Signal message missing targetId: %s", signal)
            return
        await connection_manager.send_to_participant(
            room_id,
            signal.target_id,
            {
                "type": signal.type,
                "from": user_id,
                "payload": signal.payload,
            },
        )
        return

    if signal.type == SignalMessageType.PING:
        await connection_manager.send_to_participant(
            room_id,
            user_id,
            {"type": "PONG", "timestamp": datetime.utcnow().isoformat()},
        )
        return

    await connection_manager.broadcast(
        room_id,
        {"type": signal.type, "from": user_id, "payload": signal.payload},
        exclude=user_id,
    )


def serialize_participant(participant: Participant) -> dict[str, object]:
    return {
        "participantId": participant.user_id,
        "displayName": participant.display_name,
        "role": participant.role,
        "joinedAt": participant.joined_at.isoformat(),
        "muted": participant.muted,
    }


