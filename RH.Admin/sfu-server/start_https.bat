@echo off
echo ========================================
echo   远程会诊 SFU Server (HTTPS 模式)
echo ========================================
echo.

REM 检查 Python 是否安装
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误: 未找到 Python
    echo    请先安装 Python 3.8 或更高版本
    pause
    exit /b 1
)

REM 检查虚拟环境
if not exist "venv\" (
    echo 📦 创建虚拟环境...
    python -m venv venv
    if %errorlevel% neq 0 (
        echo ❌ 虚拟环境创建失败
        pause
        exit /b 1
    )
)

REM 激活虚拟环境
echo 🔧 激活虚拟环境...
call venv\Scripts\activate.bat

REM 检查依赖
echo 📦 检查依赖...
pip show fastapi >nul 2>&1
if %errorlevel% neq 0 (
    echo 📥 安装依赖...
    pip install -r requirements.txt
    if %errorlevel% neq 0 (
        echo ❌ 依赖安装失败
        pause
        exit /b 1
    )
)

REM 检查证书文件
if not exist "dev-cert.pem" (
    echo ❌ 错误: 证书文件不存在 (dev-cert.pem)
    echo    请先运行 RH.Admin 目录下的 generate-prod-cert.bat 生成证书
    echo    然后将证书文件复制到当前目录
    pause
    exit /b 1
)

if not exist "dev-key.pem" (
    echo ❌ 错误: 私钥文件不存在 (dev-key.pem)
    echo    请先运行 RH.Admin 目录下的 generate-prod-cert.bat 生成证书
    echo    然后将私钥文件复制到当前目录
    pause
    exit /b 1
)

REM 检查 .env 文件
if not exist ".env" (
    echo ⚠️  警告: .env 文件不存在，使用默认配置
    echo    建议创建 .env 文件配置服务参数
    echo.
)

echo.
echo ✅ 准备完成，启动服务...
echo.

REM 启动服务
python start_https.py

pause

