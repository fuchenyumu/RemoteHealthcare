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

    # CORS配置
    cors_origins: list[str] = Field(
        default=["https://192.168.1.113:8848", "https://localhost:8848"],
        alias="CORS_ORIGINS"
    )
    cors_allow_credentials: bool = Field(True, alias="CORS_ALLOW_CREDENTIALS")
    cors_allow_methods: list[str] = Field(["*"], alias="CORS_ALLOW_METHODS")
    cors_allow_headers: list[str] = Field(["*"], alias="CORS_ALLOW_HEADERS")

    jwt_secret_key: str = Field("replace-with-strong-secret", alias="JWT_SECRET_KEY")
    jwt_issuer: str = Field("yinzhixin-healthcare", alias="JWT_ISSUER")
    jwt_audience: str = Field("medical-rtc", alias="JWT_AUDIENCE")
    jwt_ttl_seconds: int = Field(3600, alias="JWT_TTL_SECONDS")

    redis_host: str = Field("127.0.0.1", alias="REDIS_HOST")
    redis_port: int = Field(6379, alias="REDIS_PORT")
    redis_username: str | None = Field(default=None, alias="REDIS_USERNAME")
    redis_password: str | None = Field(default=None, alias="REDIS_PASSWORD")
    redis_db: int = Field(0, alias="REDIS_DB")
    redis_use_ssl: bool = Field(False, alias="REDIS_USE_SSL")

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


