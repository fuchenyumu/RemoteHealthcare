import asyncio
from typing import Dict, Optional

from app.models import Participant, Room, RoomState
from app.config import get_settings


class RoomManager:
    """
    In-memory manager for consultation rooms.

    后续可以替换为 Redis / PostgreSQL 持久化，这里先满足第二阶段的基础需求。
    """

    def __init__(self) -> None:
        self._rooms: Dict[str, Room] = {}
        self._participants: Dict[str, Dict[str, Participant]] = {}
        self._locks: Dict[str, asyncio.Lock] = {}
        self._settings = get_settings()

    def _get_lock(self, room_id: str) -> asyncio.Lock:
        if room_id not in self._locks:
            self._locks[room_id] = asyncio.Lock()
        return self._locks[room_id]

    async def create_room(self, room: Room) -> Room:
        async with self._get_lock(room.room_id):
            self._rooms[room.room_id] = room
            self._participants[room.room_id] = {}
            return room

    async def close_room(self, room_id: str) -> Optional[Room]:
        async with self._get_lock(room_id):
            room = self._rooms.get(room_id)
            if room:
                room.state = RoomState.CLOSED
                self._participants.pop(room_id, None)
            return room

    async def add_participant(self, room_id: str, participant: Participant) -> None:
        async with self._get_lock(room_id):
            room_participants = self._participants.setdefault(room_id, {})
            if len(room_participants) >= self._settings.max_participants_per_room:
                raise ValueError("房间人数已达上限")
            room_participants[participant.user_id] = participant

    async def remove_participant(self, room_id: str, user_id: str) -> None:
        async with self._get_lock(room_id):
            participants = self._participants.get(room_id)
            if participants and user_id in participants:
                participants.pop(user_id)

    def get_room(self, room_id: str) -> Optional[Room]:
        return self._rooms.get(room_id)

    def list_participants(self, room_id: str) -> Dict[str, Participant]:
        return self._participants.get(room_id, {}).copy()


room_manager = RoomManager()



