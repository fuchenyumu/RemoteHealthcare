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
cp env.example .env  # 配置 Redis/JWT 等变量
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

> `REDIS_HOST/PORT/PASSWORD/DB` 需指向可用的 Redis（示例：`192.168.1.202:6379,password=`）。服务启动时会尝试 `PING`，健康检查也会纳入 Redis 状态。

## REST API

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| `POST` | `/api/health/token` | 颁发 WebRTC 信令 Token，`RH.WebApi` 会代理调用，并写入会诊/成员信息。 |
| `GET` | `/api/health/status` | 返回服务健康状态、当前房间数、参与者数量及运行时长。 |
| `GET` | `/api/health/rooms` | 列出所有房间及在线成员，支持由 `RH.WebApi` 过滤会诊 ID。 |
| `GET` | `/api/health/rooms/{roomId}` | 查询单个房间详情，供前端监控/排查。 |
| `DELETE` | `/api/health/rooms/{roomId}` | 关闭/移除房间，清空 Redis 记录。 |
| `DELETE` | `/api/health/rooms/{roomId}/participants/{userId}` | 强制移除指定成员（仅更新 Redis，WebSocket 将在后续迭代补充通知）。 |

`GET /api/health/status` 会返回 `redisHealthy` 字段，当 Redis 异常时状态为 `degraded`。`GET /api/health/ready` 会在 Redis 不可用时返回 `503`，避免误判为可服务状态。

## Redis 数据结构

| Key | 类型 | 说明 |
| --- | --- | --- |
| `rc:rooms` | Set | 当前存在的房间 ID 列表 |
| `rc:room:{roomId}` | Hash | 房间元数据（会诊 ID、主持人、状态、创建/更新时间等） |
| `rc:room:{roomId}:participants` | Hash | 房间成员列表，值为参与者 JSON |
| `rc:token:{roomId}:{userId}` | String | 最近颁发的信令 Token，TTL 与 JWT 保持一致 |

所有写操作均通过 `RoomManager`/`TokenService` 完成，确保内存缓存与 Redis 同步。`ServiceStatus`、房间列表等查询接口只读取 Redis，以支持多实例部署。

## RH.WebApi 集成

`RH.WebApi` 暴露 `/api/v1/rc-rtc` 相关接口统一管理 Token 生命周期：

- `POST /rc-rtc/{id}/token`：根据当前登录用户、会诊成员角色自动生成房间号并缓存 SFU Token。
- `GET /rc-rtc/rooms` / `GET /rc-rtc/rooms/{roomId}`：查询 SFU 当前在线成员。
- `GET /rc-rtc/status`：用于 RH.Admin 仪表盘展示服务状态。

RH.Admin 通过 `src/services/medicalRtcService.ts` 连接 WebSocket (`ws://<sfu>/ws/medical/{roomId}`)，完成会诊成员可视化和音视频调度。

## 后续步骤

第二阶段后续的工作会在此服务中持续推进，包括：

- 引入 aiortc 实现真实的媒体转发能力
- 录制/回放、屏幕共享、白板等高级功能
- 健康检查、日志、监控以及部署脚本

欢迎结合 `当前阶段AGENT开发方案.md` 持续迭代。

