from functools import lru_cache
from pydantic import Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Application level settings loaded from environment variables."""

    app_name: str = Field("remote-consultation-sfu", alias="APP_NAME")
    app_env: str = Field("Development", alias="APP_ENV")
    app_host: str = Field("0.0.0.0", alias="APP_HOST")
    app_port: int = Field(8000, alias="APP_PORT")
    log_level: str = Field("INFO", alias="LOG_LEVEL")

    jwt_secret_key: str = Field(..., alias="JWT_SECRET_KEY")
    jwt_issuer: str = Field("purest-admin", alias="JWT_ISSUER")
    jwt_audience: str = Field("medical-rtc", alias="JWT_AUDIENCE")
    jwt_ttl_seconds: int = Field(3600, alias="JWT_TTL_SECONDS")

    max_participants_per_room: int = Field(12, alias="MAX_PARTICIPANTS_PER_ROOM")
    enable_recording: bool = Field(False, alias="ENABLE_RECORDING")
    recording_storage_path: str = Field("./recordings", alias="RECORDING_STORAGE_PATH")

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = False


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    """Return cached settings instance."""
    return Settings()



