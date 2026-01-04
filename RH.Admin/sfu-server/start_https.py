"""
SFU Server HTTPS 启动脚本
支持基于 SSL 证书的 HTTPS/WSS 服务
"""
import os
import sys
from pathlib import Path

import uvicorn
from app.config import get_settings

def main():
    settings = get_settings()
    
    # 获取当前脚本所在目录
    base_dir = Path(__file__).parent
    
    # 构建证书文件的完整路径
    cert_path = base_dir / settings.ssl_cert_path
    key_path = base_dir / settings.ssl_key_path
    
    # 检查证书文件是否存在
    if settings.use_https:
        if not cert_path.exists():
            print(f"❌ 错误: 证书文件不存在: {cert_path}")
            print(f"   请确保证书文件已复制到 SFU Server 目录")
            sys.exit(1)
        
        if not key_path.exists():
            print(f"❌ 错误: 私钥文件不存在: {key_path}")
            print(f"   请确保私钥文件已复制到 SFU Server 目录")
            sys.exit(1)
        
        print(f"🔍 SSL 配置检查:")
        print(f"   - 证书文件: {cert_path}")
        print(f"   - 私钥文件: {key_path}")
        print(f"   - 证书文件存在: {cert_path.exists()}")
        print(f"   - 私钥文件存在: {key_path.exists()}")
        print()
        
        # 启动 HTTPS 服务
        print(f"🚀 启动 SFU Server (HTTPS 模式)")
        print(f"   - 地址: https://{settings.app_host}:{settings.app_port}")
        print(f"   - WebSocket: wss://{settings.app_host}:{settings.app_port}/ws/medical/{{room_id}}")
        print(f"   - 环境: {settings.app_env}")
        print()
        
        uvicorn.run(
            "app.main:app",
            host=settings.app_host,
            port=settings.app_port,
            ssl_certfile=str(cert_path),
            ssl_keyfile=str(key_path),
            reload=settings.app_env.lower() == "development",
            log_level=settings.log_level.lower()
        )
    else:
        # 启动 HTTP 服务（不推荐用于生产环境）
        print(f"⚠️  启动 SFU Server (HTTP 模式 - 不建议用于生产环境)")
        print(f"   - 地址: http://{settings.app_host}:{settings.app_port}")
        print(f"   - WebSocket: ws://{settings.app_host}:{settings.app_port}/ws/medical/{{room_id}}")
        print(f"   - 环境: {settings.app_env}")
        print()
        
        uvicorn.run(
            "app.main:app",
            host=settings.app_host,
            port=settings.app_port,
            reload=settings.app_env.lower() == "development",
            log_level=settings.log_level.lower()
        )

if __name__ == "__main__":
    main()

