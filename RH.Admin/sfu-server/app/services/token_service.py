from datetime import UTC, datetime, timedelta
from typing import Any, Dict

import jwt
from fastapi import HTTPException, status

from app.config import get_settings


class TokenService:
    """Issued and validate JWT used for WebSocket signaling."""

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


token_service = TokenService()



