from __future__ import annotations

from typing import Optional

import redis.asyncio as redis

from app.config import get_settings


class RedisClient:
    """Async Redis connector shared across services."""

    def __init__(self) -> None:
        self._settings = get_settings()
        self._client: Optional[redis.Redis] = None

    async def connect(self) -> redis.Redis:
        if self._client:
            return self._client

        self._client = redis.Redis(
            host=self._settings.redis_host,
            port=self._settings.redis_port,
            username=self._settings.redis_username or None,
            password=self._settings.redis_password or None,
            db=self._settings.redis_db,
            ssl=self._settings.redis_use_ssl,
            decode_responses=True,
            health_check_interval=30,
        )
        # 验证配置是否正确
        await self._client.ping()
        return self._client

    async def close(self) -> None:
        if self._client is None:
            return
        await self._client.close()
        self._client = None

    def require_client(self) -> redis.Redis:
        if not self._client:
            raise RuntimeError("Redis client is not initialized")
        return self._client


redis_client = RedisClient()


async def get_redis() -> redis.Redis:
    """Ensure Redis connection is ready and return the client."""
    return await redis_client.connect()

