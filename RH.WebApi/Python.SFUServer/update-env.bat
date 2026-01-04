@echo off
chcp 65001 >nul
echo ========================================
echo 更新 SFU 服务器 .env 配置文件
echo ========================================
echo.

set "ENV_FILE=.env"
set "EXAMPLE_FILE=env.example"

if not exist "%ENV_FILE%" (
    echo [提示] .env 文件不存在，从 env.example 复制...
    copy "%EXAMPLE_FILE%" "%ENV_FILE%"
    echo [成功] 已创建 .env 文件
    echo.
    echo [重要] 请修改以下配置：
    echo   1. JWT_SECRET_KEY - 替换为强密码
    echo   2. REDIS_HOST - Redis 服务器地址
    echo   3. REDIS_PASSWORD - Redis 密码（如果有）
    echo   4. CORS_ORIGINS - 允许的前端地址
    echo.
    pause
    exit /b 0
)

echo [检查] 检查 .env 文件是否包含 CORS 配置...
findstr /C:"CORS_ORIGINS" "%ENV_FILE%" >nul
if %errorlevel% equ 0 (
    echo [信息] CORS 配置已存在
    echo.
    echo 当前配置：
    findstr /C:"CORS_" "%ENV_FILE%"
    echo.
    echo 如需修改，请手动编辑 .env 文件
) else (
    echo [警告] 未找到 CORS 配置，正在添加...
    echo. >> "%ENV_FILE%"
    echo # CORS配置（JSON数组格式） >> "%ENV_FILE%"
    echo CORS_ORIGINS=["https://192.168.1.113:8848", "https://localhost:8848"] >> "%ENV_FILE%"
    echo CORS_ALLOW_CREDENTIALS=true >> "%ENV_FILE%"
    echo CORS_ALLOW_METHODS=["*"] >> "%ENV_FILE%"
    echo CORS_ALLOW_HEADERS=["*"] >> "%ENV_FILE%"
    echo [成功] 已添加 CORS 配置
)

echo.
echo [检查] 检查 JWT_ISSUER 配置...
findstr /C:"JWT_ISSUER=yinzhixin-healthcare" "%ENV_FILE%" >nul
if %errorlevel% neq 0 (
    echo [警告] JWT_ISSUER 未更新为 yinzhixin-healthcare
    echo [提示] 请手动修改 .env 文件中的 JWT_ISSUER 值
)

echo.
echo ========================================
echo 配置检查完成！
echo ========================================
echo.
echo 重要提醒：
echo 1. 请确保 JWT_SECRET_KEY 使用强密码
echo 2. 修改 CORS_ORIGINS 中的 IP 地址为实际前端地址
echo 3. 配置 Redis 连接信息
echo.
pause
