from datetime import datetime
from enum import Enum
from typing import Any, Dict, Optional

from pydantic import BaseModel, Field, ConfigDict


class ApiModel(BaseModel):
    """Shared config to allow using snake_case names when aliases exist."""

    model_config = ConfigDict(populate_by_name=True)


class ParticipantRole(str, Enum):
    HOST = "HOST"
    EXPERT = "EXPERT"
    OBSERVER = "OBSERVER"


class Participant(ApiModel):
    """Participant metadata stored in room manager."""

    user_id: str = Field(..., description="用户唯一标识", serialization_alias="participantId")
    display_name: str = Field(..., description="显示名称", serialization_alias="displayName")
    role: ParticipantRole = Field(ParticipantRole.EXPERT, description="会诊角色")
    joined_at: datetime = Field(default_factory=datetime.utcnow, serialization_alias="joinedAt")
    muted: bool = False


class RoomState(str, Enum):
    CREATED = "CREATED"
    ACTIVE = "ACTIVE"
    CLOSED = "CLOSED"


class Room(ApiModel):
    """Minimal room description for signaling层."""

    room_id: str
    consultation_id: int
    host_id: str
    state: RoomState = RoomState.CREATED
    created_at: datetime = Field(default_factory=datetime.utcnow)
    updated_at: datetime = Field(default_factory=datetime.utcnow)
    closed_at: Optional[datetime] = None


class JoinRoomPayload(ApiModel):
    """Payload sent by客户端以加入房间."""

    token: str
    device: Optional[str] = None
    sdk_version: Optional[str] = None


class HealthResponse(ApiModel):
    status: str = Field(default="healthy")
    timestamp: datetime = Field(default_factory=datetime.utcnow)
    app: str


class SignalMessageType(str, Enum):
    PING = "PING"
    OFFER = "OFFER"
    ANSWER = "ANSWER"
    CANDIDATE = "CANDIDATE"
    CUSTOM = "CUSTOM"


class SignalMessage(ApiModel):
    """WebSocket signaling消息体."""

    type: SignalMessageType
    target_id: Optional[str] = Field(default=None, alias="targetId")
    payload: Dict[str, Any] = Field(default_factory=dict)


class TokenRequest(ApiModel):
    room_id: str = Field(
        ...,
        validation_alias="roomId",
        serialization_alias="roomId",
    )
    consultation_id: int = Field(
        ...,
        validation_alias="consultationId",
        serialization_alias="consultationId",
    )
    user_id: str = Field(
        ...,
        validation_alias="userId",
        serialization_alias="userId",
    )
    display_name: str = Field(
        ...,
        validation_alias="displayName",
        serialization_alias="displayName",
    )
    role: ParticipantRole = ParticipantRole.EXPERT
    host_id: Optional[str] = Field(
        default=None,
        validation_alias="hostId",
        serialization_alias="hostId",
    )


class TokenResponse(ApiModel):
    token: str
    expires_in: int = Field(
        ...,
        validation_alias="expiresIn",
        serialization_alias="expiresIn",
    )


class ParticipantPublic(ApiModel):
    participant_id: str = Field(
        ...,
        validation_alias="participantId",
        serialization_alias="participantId",
    )
    display_name: str = Field(
        ...,
        validation_alias="displayName",
        serialization_alias="displayName",
    )
    role: ParticipantRole
    joined_at: datetime = Field(
        ...,
        validation_alias="joinedAt",
        serialization_alias="joinedAt",
    )
    muted: bool


class RoomSnapshot(ApiModel):
    room_id: str = Field(
        ...,
        validation_alias="roomId",
        serialization_alias="roomId",
    )
    consultation_id: int = Field(
        ...,
        validation_alias="consultationId",
        serialization_alias="consultationId",
    )
    host_id: str = Field(
        ...,
        validation_alias="hostId",
        serialization_alias="hostId",
    )
    state: RoomState
    participant_count: int = Field(
        ...,
        validation_alias="participantCount",
        serialization_alias="participantCount",
    )
    created_at: datetime = Field(
        ...,
        validation_alias="createdAt",
        serialization_alias="createdAt",
    )
    updated_at: datetime = Field(
        ...,
        validation_alias="updatedAt",
        serialization_alias="updatedAt",
    )
    closed_at: Optional[datetime] = Field(
        default=None,
        validation_alias="closedAt",
        serialization_alias="closedAt",
    )
    participants: list[ParticipantPublic] = Field(default_factory=list)


class ServiceStatusResponse(ApiModel):
    status: str
    app: str
    room_count: int = Field(
        ...,
        validation_alias="roomCount",
        serialization_alias="roomCount",
    )
    participant_count: int = Field(
        ...,
        validation_alias="participantCount",
        serialization_alias="participantCount",
    )
    started_at: datetime = Field(
        ...,
        validation_alias="startedAt",
        serialization_alias="startedAt",
    )
    uptime_seconds: int = Field(
        ...,
        validation_alias="uptimeSeconds",
        serialization_alias="uptimeSeconds",
    )
    redis_healthy: bool = Field(
        ...,
        validation_alias="redisHealthy",
        serialization_alias="redisHealthy",
    )

