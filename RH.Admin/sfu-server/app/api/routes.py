from fastapi import APIRouter

from app.config import get_settings
from app.models import HealthResponse

router = APIRouter()
settings = get_settings()


@router.get("/", response_model=HealthResponse, summary="健康检查")
async def health_check() -> HealthResponse:
    return HealthResponse(status="healthy", app=settings.app_name)


@router.get("/ready", response_model=HealthResponse, summary="就绪检查")
async def readiness_check() -> HealthResponse:
    # TODO: 增加 Redis / aiortc / 录制等依赖检查
    return HealthResponse(status="ready", app=settings.app_name)



