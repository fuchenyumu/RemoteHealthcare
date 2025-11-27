from __future__ import annotations

from datetime import UTC, datetime, timedelta
from typing import Any, Dict, Optional, Tuple

import jwt
from fastapi import HTTPException, status

from app.config import get_settings
from app.services.redis_client import get_redis


class TokenService:
    """Issued and validate JWT used for WebSocket signaling."""

    TOKEN_KEY_TEMPLATE = "rc:token:{room_id}:{user_id}"

    def __init__(self) -> None:
        self._settings = get_settings()

    def create_token(self, payload: Dict[str, Any]) -> str:
        now = datetime.now(tz=UTC)
        exp = now + timedelta(seconds=self._settings.jwt_ttl_seconds)
        token_payload = {
            "iss": self._settings.jwt_issuer,
            "aud": self._settings.jwt_audience,
            "iat": now,
            "nbf": now,
            "exp": exp,
            **payload,
        }
        return jwt.encode(
            token_payload,
            self._settings.jwt_secret_key,
            algorithm="HS256",
        )

    def verify_token(self, token: str) -> Dict[str, Any]:
        try:
            return jwt.decode(
                token,
                self._settings.jwt_secret_key,
                algorithms=["HS256"],
                audience=self._settings.jwt_audience,
                issuer=self._settings.jwt_issuer,
            )
        except jwt.ExpiredSignatureError as exc:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED, detail="Token 已过期"
            ) from exc
        except jwt.InvalidTokenError as exc:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED, detail="Token 无效"
            ) from exc

    async def get_cached_token(self, room_id: str, user_id: str) -> Tuple[Optional[str], Optional[int]]:
        redis = await get_redis()
        key = self._token_key(room_id, user_id)
        token = await redis.get(key)
        if token is None:
            return None, None
        ttl = await redis.ttl(key)
        ttl = ttl if ttl > 0 else self._settings.jwt_ttl_seconds
        return token, int(ttl)

    async def cache_token(self, room_id: str, user_id: str, token: str) -> None:
        redis = await get_redis()
        await redis.set(
            self._token_key(room_id, user_id),
            token,
            ex=self._cache_ttl(),
        )

    async def revoke_token(self, room_id: str, user_id: str) -> None:
        redis = await get_redis()
        await redis.delete(self._token_key(room_id, user_id))

    def _token_key(self, room_id: str, user_id: str) -> str:
        return self.TOKEN_KEY_TEMPLATE.format(room_id=room_id, user_id=user_id)

    def _cache_ttl(self) -> int:
        # 预留 15 秒 buffer，避免 token 刚好过期
        ttl = max(self._settings.jwt_ttl_seconds - 15, 30)
        return ttl


token_service = TokenService()
