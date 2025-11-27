import json
import logging
from typing import Any, Dict

from fastapi import WebSocket, WebSocketDisconnect

from app.models import JoinRoomPayload, Participant, ParticipantRole, Room
from app.services.room_manager import room_manager
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

        room = room_manager.get_room(room_id)
        if not room:
            room = Room(
                room_id=room_id,
                consultation_id=int(token_claims["consultation_id"]),
                host_id=str(token_claims.get("host_id", participant.user_id)),
            )
            await room_manager.create_room(room)

        await room_manager.add_participant(room_id, participant)
        await websocket.send_json(
            {"type": "JOINED", "participantId": participant.user_id, "roomId": room_id}
        )

        # 信令循环（待结合 aiortc）
        while True:
            message = await websocket.receive_text()
            await handle_signaling_message(room_id, participant.user_id, message)
            await websocket.send_json({"type": "ACK", "echo": message})

    except WebSocketDisconnect:
        logger.info("WebSocket disconnected for room %s", room_id)
    except Exception as exc:  # pylint: disable=broad-except
        logger.exception("Signaling error: %s", exc)
        await websocket.close(code=4000)
    finally:
        if participant:
            await room_manager.remove_participant(room_id, participant.user_id)


async def handle_signaling_message(room_id: str, user_id: str, message: str) -> None:
    """
    预处理客户端发送的信令消息。
    当前仅做日志记录，后续将接入 aiortc 的实际处理逻辑。
    """

    try:
        payload: Dict[str, Any] = json.loads(message)
    except json.JSONDecodeError:
        logger.warning("Invalid JSON message from %s: %s", user_id, message)
        return

    logger.debug("Signaling message in room %s from %s: %s", room_id, user_id, payload)
    # TODO: 转发 offer/answer/candidate 给其他参与者或 SFU



