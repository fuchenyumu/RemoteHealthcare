# 远程会诊 SFU 服务

该服务提供远程会诊场景下的实时音视频信令、房间管理以及质量监控能力，配合 `RH.WebApi` 与 `RH.Admin` 完成第二阶段目标。

## 功能概要

- FastAPI + WebSocket 信令服务
- aiortc 驱动的 SFU 媒体转发
- JWT 鉴权与会诊房间权限校验
- 质量监控、录制、屏幕共享等扩展接口（待逐步实现）

## 目录结构

```
sfu-server/
├── app
│   ├── api              # REST API（健康检查、会话管理等）
│   ├── config.py        # 配置与环境变量
│   ├── main.py          # FastAPI 入口
│   ├── models.py        # 信令相关模型
│   └── services         # 房间、Token、质量监控服务
├── Dockerfile           # 容器化配置
├── requirements.txt     # Python 依赖
└── .env.example         # 环境变量示例
```

## 快速开始

```bash
pip install -r requirements.txt
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

## 后续步骤

第二阶段后续的工作会在此服务中持续推进，包括：

- 引入 aiortc 实现真实的媒体转发能力
- 录制/回放、屏幕共享、白板等高级功能
- 健康检查、日志、监控以及部署脚本

欢迎结合 `当前阶段AGENT开发方案.md` 持续迭代。


