@echo off
echo 正在生成局域网HTTPS证书...
echo.

REM 检测 OpenSSL
set "OPENSSL_CMD="
if exist "C:\Program Files\OpenSSL-Win64\bin\openssl.exe" (
    set "OPENSSL_CMD=C:\Program Files\OpenSSL-Win64\bin\openssl.exe"
) else if exist "C:\Program Files\Git\usr\bin\openssl.exe" (
    set "OPENSSL_CMD=C:\Program Files\Git\usr\bin\openssl.exe"
) else (
    where openssl >nul 2>&1
    if %errorlevel% equ 0 (set "OPENSSL_CMD=openssl") else (
        echo ❌ 未找到 OpenSSL
        pause
        exit /b 1
    )
)

REM 配置变量
set "IP=192.168.1.113"
set "PORT=8848"
set "CERT_NAME=dev-cert"
set "KEY_NAME=dev-key"

echo 使用IP: %IP%
echo 端口: %PORT%
echo.

REM 1. 生成私钥
"%OPENSSL_CMD%" genrsa -out %KEY_NAME%.pem 2048

REM 2. 创建精简配置（仅IP）
(
echo [req]
echo default_bits = 2048
echo prompt = no
echo default_md = sha256
echo distinguished_name = dn
echo x509_extensions = v3_req
echo.
echo [dn]
echo C = CN
echo ST = Beijing
echo O = LocalHealthcare
echo CN = %IP%
echo.
echo [v3_req]
echo subjectAltName = @alt_names
echo keyUsage = digitalSignature, keyEncipherment
echo extendedKeyUsage = serverAuth
echo.
echo [alt_names]
echo IP.1 = %IP%
echo IP.2 = 127.0.0.1
echo DNS.1 = localhost
) > openssl.cnf

REM 3. 生成自签名证书
"%OPENSSL_CMD%" req -new -x509 -key %KEY_NAME%.pem -out %CERT_NAME%.pem -days 365 -config openssl.cnf -extensions v3_req

REM 4. 生成 PFX（.NET Core 需要）
"%OPENSSL_CMD%" pkcs12 -export -out %CERT_NAME%.pfx -inkey %KEY_NAME%.pem -in %CERT_NAME%.pem -password pass:

REM 5. 清理
del openssl.cnf

echo.
echo ✅ 证书生成成功！
echo 📜 证书: %CERT_NAME%.pem, %CERT_NAME%.pfx
echo 🔑 私钥: %KEY_NAME%.pem
echo.
echo 支持地址:
echo   - https://%IP%:%PORT%
echo   - https://localhost:%PORT%
echo   - https://127.0.0.1:%PORT%
echo.
echo 重要步骤:
echo   1. 将 %CERT_NAME%.pfx 复制到 .NET 项目
echo   2. 将 %CERT_NAME%.pem 复制到 Vue 项目
echo   3. 访问 https://%IP%:%PORT% 并信任证书
echo.
pause
