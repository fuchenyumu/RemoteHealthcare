from datetime import datetime
from enum import Enum
from typing import Optional

from pydantic import BaseModel, Field


class ParticipantRole(str, Enum):
    HOST = "HOST"
    EXPERT = "EXPERT"
    OBSERVER = "OBSERVER"


class Participant(BaseModel):
    """Participant metadata stored in room manager."""

    user_id: str = Field(..., description="用户唯一标识")
    display_name: str = Field(..., description="显示名称")
    role: ParticipantRole = Field(ParticipantRole.EXPERT, description="会诊角色")
    joined_at: datetime = Field(default_factory=datetime.utcnow)
    muted: bool = False


class RoomState(str, Enum):
    CREATED = "CREATED"
    ACTIVE = "ACTIVE"
    CLOSED = "CLOSED"


class Room(BaseModel):
    """Minimal room description for signaling层."""

    room_id: str
    consultation_id: int
    host_id: str
    state: RoomState = RoomState.CREATED
    created_at: datetime = Field(default_factory=datetime.utcnow)
    closed_at: Optional[datetime] = None


class JoinRoomPayload(BaseModel):
    """Payload sent by客户端以加入房间."""

    token: str
    device: Optional[str] = None
    sdk_version: Optional[str] = None


class HealthResponse(BaseModel):
    status: str = Field(default="healthy")
    timestamp: datetime = Field(default_factory=datetime.utcnow)
    app: str



