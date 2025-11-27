from typing import Dict

from fastapi import WebSocket


class ConnectionManager:
    """Manage active WebSocket connections for each room."""

    def __init__(self) -> None:
        self._connections: Dict[str, Dict[str, WebSocket]] = {}

    async def connect(self, room_id: str, user_id: str, websocket: WebSocket) -> None:
        room_connections = self._connections.setdefault(room_id, {})
        room_connections[user_id] = websocket

    async def disconnect(self, room_id: str, user_id: str) -> None:
        room_connections = self._connections.get(room_id)
        if room_connections and user_id in room_connections:
            room_connections.pop(user_id)
            if not room_connections:
                self._connections.pop(room_id, None)

    async def send_to_participant(
        self, room_id: str, target_user_id: str, message: dict
    ) -> None:
        room_connections = self._connections.get(room_id)
        if not room_connections:
            return

        websocket = room_connections.get(target_user_id)
        if websocket:
            await websocket.send_json(message)

    async def broadcast(self, room_id: str, message: dict, exclude: str | None = None) -> None:
        room_connections = self._connections.get(room_id)
        if not room_connections:
            return

        for user_id, websocket in room_connections.items():
            if exclude and user_id == exclude:
                continue
            await websocket.send_json(message)


connection_manager = ConnectionManager()



