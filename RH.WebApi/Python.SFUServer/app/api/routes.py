from __future__ import annotations

import logging
from datetime import datetime

from fastapi import APIRouter, HTTPException, Query, Response, status

from app.config import get_settings
from app.models import (
    HealthResponse,
    ParticipantPublic,
    Room,
    RoomSnapshot,
    ServiceStatusResponse,
    TokenRequest,
    TokenResponse,
)
from app.services.redis_client import get_redis
from app.services.room_manager import room_manager
from app.services.token_service import token_service

router = APIRouter()
settings = get_settings()
SERVICE_STARTED_AT = datetime.utcnow()
logger = logging.getLogger(__name__)


@router.get("/", response_model=HealthResponse, summary="健康检查")
async def health_check() -> HealthResponse:
    return HealthResponse(status="healthy", app=settings.app_name)


@router.get("/ready", response_model=HealthResponse, summary="就绪检查")
async def readiness_check() -> HealthResponse:
    try:
        redis = await get_redis()
        await redis.ping()
    except Exception as exc:  # pylint: disable=broad-except
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail=f"Redis 未就绪: {exc}",
        ) from exc
    return HealthResponse(status="ready", app=settings.app_name)


@router.post(
    "/token",
    response_model=TokenResponse,
    summary="颁发 WebRTC 信令 Token",
)
async def issue_token(
    payload: TokenRequest,
    force_refresh: bool = Query(False, alias="forceRefresh"),
) -> TokenResponse:
    room = Room(
        room_id=payload.room_id,
        consultation_id=payload.consultation_id,
        host_id=payload.host_id or payload.user_id,
    )
    await room_manager.ensure_room_exists(room)

    if not force_refresh:
        cached_token, ttl = await token_service.get_cached_token(payload.room_id, payload.user_id)
        if cached_token and ttl and ttl > 0:
            return TokenResponse(token=cached_token, expiresIn=ttl)

    token = token_service.create_token(
        {
            "sub": payload.user_id,
            "name": payload.display_name,
            "role": payload.role,
            "room_id": payload.room_id,
            "consultation_id": payload.consultation_id,
            "host_id": payload.host_id or payload.user_id,
        }
    )
    await token_service.cache_token(payload.room_id, payload.user_id, token)
    return TokenResponse(token=token, expiresIn=settings.jwt_ttl_seconds)


@router.get(
    "/status",
    response_model=ServiceStatusResponse,
    summary="SFU 服务状态",
)
async def service_status() -> ServiceStatusResponse:
    redis_healthy = True
    participant_count = 0
    rooms = {}
    try:
        rooms = await room_manager.list_rooms()
        participant_count = await room_manager.total_participant_count()
    except Exception as exc:  # pylint: disable=broad-except
        redis_healthy = False
        logger.warning("读取房间状态失败：%s", exc)

    uptime = int((datetime.utcnow() - SERVICE_STARTED_AT).total_seconds())
    return ServiceStatusResponse(
        status="healthy" if redis_healthy else "degraded",
        app=settings.app_name,
        room_count=len(rooms),
        participant_count=participant_count,
        started_at=SERVICE_STARTED_AT,
        uptime_seconds=uptime,
        redis_healthy=redis_healthy,
    )


@router.get(
    "/rooms",
    response_model=list[RoomSnapshot],
    summary="房间列表",
)
async def list_rooms() -> list[RoomSnapshot]:
    snapshots: list[RoomSnapshot] = []
    rooms = await room_manager.list_rooms()
    for room_id, room in rooms.items():
        snapshots.append(await build_room_snapshot(room_id, room))
    return snapshots


@router.get(
    "/rooms/{room_id}",
    response_model=RoomSnapshot,
    summary="房间详情",
)
async def get_room(room_id: str) -> RoomSnapshot:
    room = await room_manager.get_room(room_id)
    if not room:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="房间不存在")
    return await build_room_snapshot(room_id, room)


@router.delete(
    "/rooms/{room_id}",
    response_model=RoomSnapshot,
    summary="关闭并移除房间",
)
async def delete_room(room_id: str) -> RoomSnapshot:
    room = await room_manager.close_room(room_id)
    if not room:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="房间不存在")

    return RoomSnapshot(
        room_id=room.room_id,
        consultation_id=room.consultation_id,
        host_id=room.host_id,
        state=room.state,
        participant_count=0,
        created_at=room.created_at,
        updated_at=room.updated_at,
        closed_at=room.closed_at,
        participants=[],
    )


@router.delete(
    "/rooms/{room_id}/participants/{user_id}",
    status_code=status.HTTP_204_NO_CONTENT,
    summary="移除房间成员",
    response_class=Response,
)
async def remove_participant(room_id: str, user_id: str) -> Response:
    removed = await room_manager.force_remove_participant(room_id, user_id)
    if not removed:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="房间或成员不存在",
        )
    # TODO: 若该成员在线，考虑主动通知 WebSocket 断开
    return Response(status_code=status.HTTP_204_NO_CONTENT)


async def build_room_snapshot(room_id: str, room: Room) -> RoomSnapshot:
    participants = await room_manager.list_participants(room_id)
    participants_public = [
        ParticipantPublic(
            participant_id=p.user_id,
            display_name=p.display_name,
            role=p.role,
            joined_at=p.joined_at,
            muted=p.muted,
        )
        for p in participants.values()
    ]
    return RoomSnapshot(
        room_id=room.room_id,
        consultation_id=room.consultation_id,
        host_id=room.host_id,
        state=room.state,
        participant_count=len(participants_public),
        created_at=room.created_at,
        updated_at=room.updated_at,
        closed_at=room.closed_at,
        participants=participants_public,
    )
