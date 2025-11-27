import logging
from contextlib import asynccontextmanager
from typing import AsyncIterator

from fastapi import FastAPI, WebSocket
from fastapi.middleware.cors import CORSMiddleware

from app.api import api_router
from app.config import get_settings
from app.services.redis_client import redis_client
from app.ws.signaling import medical_signaling_endpoint

settings = get_settings()
logging.basicConfig(level=settings.log_level)


@asynccontextmanager
async def lifespan(_: FastAPI) -> AsyncIterator[None]:
    logging.info("Starting SFU service in %s mode", settings.app_env)
    try:
        await redis_client.connect()
        logging.info("Redis connection established")
    except Exception as exc:  # pylint: disable=broad-except
        logging.warning("Redis connection failed: %s", exc)
    yield
    await redis_client.close()
    logging.info("Stopping SFU service")


app = FastAPI(
    title="Remote Consultation SFU",
    description="远程会诊实时音视频服务",
    version="0.1.0",
    lifespan=lifespan,
)

app.include_router(api_router, prefix="/api")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.websocket("/ws/medical/{room_id}")
async def websocket_medical(room_id: str, websocket: WebSocket) -> None:  # pragma: no cover
    await medical_signaling_endpoint(websocket, room_id)


@app.get("/")
async def root() -> dict[str, str]:
    return {"message": "Remote consultation SFU is running"}


