from __future__ import annotations

import asyncio
import json
from datetime import datetime
from typing import Dict, Optional, Any

from app.config import get_settings
from app.models import Participant, Room, RoomState
from app.services.redis_client import get_redis


class RoomManager:
    """
    房间与成员管理。

    - 内存中保留极简缓存以服务当前进程的信令。
    - 真实数据写入 Redis，便于多实例共享与监控查询。
    """

    ROOM_SET_KEY = "rc:rooms"
    ROOM_KEY_PREFIX = "rc:room:"
    PARTICIPANTS_SUFFIX = ":participants"

    def __init__(self) -> None:
        self._rooms: Dict[str, Room] = {}
        self._participants: Dict[str, Dict[str, Participant]] = {}
        self._locks: Dict[str, asyncio.Lock] = {}
        self._settings = get_settings()

    def _get_lock(self, room_id: str) -> asyncio.Lock:
        if room_id not in self._locks:
            self._locks[room_id] = asyncio.Lock()
        return self._locks[room_id]

    def _room_key(self, room_id: str) -> str:
        return f"{self.ROOM_KEY_PREFIX}{room_id}"

    def _participants_key(self, room_id: str) -> str:
        return f"{self._room_key(room_id)}{self.PARTICIPANTS_SUFFIX}"

    async def create_room(self, room: Room) -> Room:
        async with self._get_lock(room.room_id):
            room.state = RoomState.CREATED
            room.created_at = datetime.utcnow()
            room.updated_at = room.created_at
            self._rooms[room.room_id] = room
            self._participants[room.room_id] = {}

            redis = await get_redis()
            await self._hset(redis, self._room_key(room.room_id), self._serialize_room(room))
            await redis.sadd(self.ROOM_SET_KEY, room.room_id)
            await redis.delete(self._participants_key(room.room_id))
            return room

    async def ensure_room_exists(self, room: Room) -> Room:
        """确保房间存在于 Redis 中（供 Token 接口调用）。"""
        cached = await self.get_room(room.room_id)
        if cached:
            return cached
        return await self.create_room(room)

    async def close_room(self, room_id: str) -> Optional[Room]:
        async with self._get_lock(room_id):
            room = await self.get_room(room_id)
            if not room:
                return None

            room.state = RoomState.CLOSED
            room.closed_at = datetime.utcnow()
            room.updated_at = room.closed_at
            self._rooms.pop(room_id, None)
            self._participants.pop(room_id, None)

            redis = await get_redis()
            await self._hset(
                redis,
                self._room_key(room_id),
                {
                    "state": room.state.value,
                    "updated_at": room.updated_at.isoformat(),
                    "closed_at": room.closed_at.isoformat(),
                },
            )
            await redis.delete(self._participants_key(room_id))
            await redis.srem(self.ROOM_SET_KEY, room_id)
            return room

    async def add_participant(self, room_id: str, participant: Participant) -> None:
        async with self._get_lock(room_id):
            room_participants = self._participants.setdefault(room_id, {})
            redis = await get_redis()
            current_count = await redis.hlen(self._participants_key(room_id))
            if max(len(room_participants), current_count) >= self._settings.max_participants_per_room:
                raise ValueError("房间人数已达上限")

            room_participants[participant.user_id] = participant
            room = await self.get_room(room_id)
            if room and room.state != RoomState.CLOSED:
                room.state = RoomState.ACTIVE
                room.updated_at = datetime.utcnow()

            await self._hset(
                redis,
                self._participants_key(room_id),
                {
                    participant.user_id: json.dumps(participant.model_dump(mode="json")),
                },
            )
            if room:
                await self._hset(
                    redis,
                    self._room_key(room_id),
                    {
                        "state": room.state.value,
                        "updated_at": room.updated_at.isoformat(),
                    },
                )

    async def remove_participant(self, room_id: str, user_id: str) -> bool:
        async with self._get_lock(room_id):
            participants = self._participants.get(room_id)
            removed_local = False
            if participants and user_id in participants:
                participants.pop(user_id)
                removed_local = True

            redis = await get_redis()
            removed_redis = await redis.hdel(self._participants_key(room_id), user_id)
            removed = removed_local or removed_redis > 0
            participants_in_room = await redis.hlen(self._participants_key(room_id))
            if participants_in_room == 0:
                room = await self.get_room(room_id)
                if room and room.state != RoomState.CLOSED:
                    room.state = RoomState.CREATED
                    room.updated_at = datetime.utcnow()
                    await self._hset(
                        redis,
                        self._room_key(room_id),
                        {
                            "state": room.state.value,
                            "updated_at": room.updated_at.isoformat(),
                        },
                    )
            return removed

    async def get_room(self, room_id: str) -> Optional[Room]:
        room = self._rooms.get(room_id)
        if room:
            return room

        redis = await get_redis()
        payload = await redis.hgetall(self._room_key(room_id))
        if not payload:
            return None
        room = self._deserialize_room(payload)
        self._rooms[room_id] = room
        self._participants.setdefault(room_id, {})
        return room

    async def list_participants(self, room_id: str) -> Dict[str, Participant]:
        participants = self._participants.get(room_id)
        if participants:
            return participants.copy()

        redis = await get_redis()
        data = await redis.hgetall(self._participants_key(room_id))
        parsed: Dict[str, Participant] = {}
        for user_id, payload in data.items():
            parsed[user_id] = Participant.model_validate_json(payload)
        self._participants[room_id] = parsed
        return parsed.copy()

    async def list_rooms(self) -> Dict[str, Room]:
        redis = await get_redis()
        room_ids = list(await redis.smembers(self.ROOM_SET_KEY))
        if not room_ids:
            return {}

        pipeline = redis.pipeline()
        for room_id in room_ids:
            pipeline.hgetall(self._room_key(room_id))
        raw_rooms = await pipeline.execute()

        rooms: Dict[str, Room] = {}
        for room_id, payload in zip(room_ids, raw_rooms):
            if not payload:
                continue
            room = self._deserialize_room(payload)
            self._rooms[room_id] = room
            rooms[room_id] = room
        return rooms

    async def total_participant_count(self) -> int:
        redis = await get_redis()
        room_ids = list(await redis.smembers(self.ROOM_SET_KEY))
        if not room_ids:
            return 0
        pipeline = redis.pipeline()
        for room_id in room_ids:
            pipeline.hlen(self._participants_key(room_id))
        counts = await pipeline.execute()
        return int(sum(counts))

    async def force_remove_participant(self, room_id: str, user_id: str) -> bool:
        """
        供管理接口使用，删除成员并同步 Redis。
        若在线 WebSocket 存在，由外层调用者负责通知并主动断开。
        """

        return await self.remove_participant(room_id, user_id)

    def _serialize_room(self, room: Room) -> dict[str, str]:
        return {
            "room_id": room.room_id,
            "consultation_id": str(room.consultation_id),
            "host_id": room.host_id,
            "state": room.state.value,
            "created_at": room.created_at.isoformat(),
            "updated_at": room.updated_at.isoformat(),
            "closed_at": room.closed_at.isoformat() if room.closed_at else "",
        }

    async def _hset(self, redis, key: str, mapping: dict[str, Any]) -> None:
        """展开 key/value 并直接执行 HSET，兼容不同 redis-py 版本的签名。"""
        if not mapping:
            return
        flat: list[str] = []
        for field, value in mapping.items():
            if value is None:
                continue
            if isinstance(value, datetime):
                value = value.isoformat()
            else:
                value = str(value)
            flat.extend([field, value])
        if not flat:
            return

        if len(flat) <= 2:
            await redis.execute_command("HSET", key, *flat)
            return

        # 兼容旧版 Redis（仅支持单字段 HSET），改用 HMSET 写入多字段
        await redis.execute_command("HMSET", key, *flat)

    def _deserialize_room(self, payload: dict[str, str]) -> Room:
        created_at = (
            datetime.fromisoformat(payload["created_at"])
            if payload.get("created_at")
            else datetime.utcnow()
        )
        closed_at = (
            datetime.fromisoformat(payload["closed_at"])
            if payload.get("closed_at")
            else None
        )
        updated_at = (
            datetime.fromisoformat(payload["updated_at"])
            if payload.get("updated_at")
            else datetime.utcnow()
        )
        return Room(
            room_id=payload["room_id"],
            consultation_id=int(payload["consultation_id"]),
            host_id=payload["host_id"],
            state=RoomState(payload.get("state", RoomState.CREATED.value)),
            created_at=created_at,
            updated_at=updated_at,
            closed_at=closed_at,
        )


room_manager = RoomManager()
