详细拆解在 Windows + Vue 3 + .NET 8 环境下，使用 aiortc 搭建本地 SFU 服务器并完成前后端接入的完整方案。
方案总览
本方案将分为三个部分：

    后端 SFU 服务：使用 aiortc 和 FastAPI 在 Python 中搭建一个轻量级的 SFU 服务器。
    前端 Vue 3 集成：创建一个 Vue 3 组件，通过 WebSocket 与 SFU 服务器通信，并使用 WebRTC API 进行音视频流的采集和渲染。
    .NET 8 后端集成（可选）：说明如何让你的 .NET 8 后端参与进来，例如进行房间管理、用户认证等。

第一部分：部署本地 SFU 服务器 (Python)
步骤 1: 安装 Python 环境
如果你的 Windows 机器上没有安装 Python，请先下载并安装。

    访问 Python 官网，下载 Python 3.10 或更高版本（推荐 3.11）。
    重要：在安装界面，请务必勾选 “Add Python.exe to PATH”。
    安装完成后，打开命令提示符（CMD）或 PowerShell，输入 python --version 和 pip --version，确认安装成功。

步骤 2: 创建项目并安装依赖

    创建一个新的文件夹作为你的 SFU 服务器项目目录，例如 sfu-server。
    打开 CMD，进入该目录：
    bash

运行

cd path\to\sfu-server

创建并激活一个虚拟环境（推荐，避免污染全局环境）：
bash
运行

python -m venv venv
.\venv\Scripts\activate

激活后，你的命令行提示符前面会有 (venv) 字样。
安装所需的 Python 库：
bash
运行

    pip install aiortc fastapi "uvicorn[standard]" python-multipart

        aiortc: 核心的 WebRTC 和 SFU 库。
        fastapi: 一个现代、快速的 Web 框架，用于构建 API。
        uvicorn: 一个 ASGI 服务器，用于运行 FastAPI 应用。
        python-multipart: 用于处理文件上传（本示例可能用不到，但安装它是个好习惯）。

步骤 3: 编写 SFU 服务器代码
在 sfu-server 目录下，创建一个名为 main.py 的文件，并粘贴以下代码。这份代码实现了一个简单的 SFU 逻辑：接收一个客户端的流，并将其转发给房间内的所有其他客户端。
python
运行

import json
from typing import Dict, Set
from fastapi import FastAPI, WebSocket, WebSocketDisconnect, Request
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from aiortc import RTCPeerConnection, RTCSessionDescription, VideoStreamTrack
from aiortc.contrib.media import MediaBlackhole

# 创建 FastAPI 应用
app = FastAPI()

# 为了方便演示，我们可以让 FastAPI  serve 静态文件和一个简单的 HTML 页面
# 这一步不是必须的，因为你的前端是 Vue 3 项目
app.mount("/static", StaticFiles(directory="static"), name="static")
templates = Jinja2Templates(directory="templates")

# 存储所有活跃的 PeerConnections
# key: room_id, value: Set[RTCPeerConnection]
rooms: Dict[str, Set[RTCPeerConnection]] = {}

# 媒体黑洞，用于接收不需要转发的流（例如，当只有一个人在房间里时）
consumer = MediaBlackhole()

@app.get("/")
async def get(request: Request):
    """提供一个简单的 HTML 测试页面（可选）"""
    return templates.TemplateResponse("index.html", {"request": request})

@app.websocket("/ws/{room_id}/{client_id}")
async def websocket_endpoint(websocket: WebSocket, room_id: str, client_id: str):
    await websocket.accept()
    print(f"Client {client_id} connected to room {room_id}")

    # 创建一个新的 RTCPeerConnection
    pc = RTCPeerConnection()
    
    # 将 pc 添加到房间
    if room_id not in rooms:
        rooms[room_id] = set()
    rooms[room_id].add(pc)

    @pc.on("track")
    async def on_track(track):
        """当收到一个媒体流时触发"""
        print(f"Track {track.kind} received from {client_id}")

        # 如果是视频流，我们需要创建一个新的 VideoStreamTrack 来转发
        # 这是 SFU 核心逻辑的简化版本：将收到的轨道分发给房间内其他人
        if track.kind == "video":
            # 遍历房间内所有其他 peer
            for other_pc in rooms[room_id]:
                if other_pc != pc and other_pc.connectionState == "connected":
                    # 检查 other_pc 是否已经在接收这个流了，避免重复添加
                    if not any(t for t in other_pc.getTransceivers() if t.receiver.track and t.receiver.track.id == track.id):
                        print(f"Forwarding track {track.id} to {other_pc}")
                        other_transceiver = other_pc.addTransceiver(track.kind, direction="recvonly")
                        await other_transceiver.setRemoteDescription(RTCSessionDescription(sdp="", type="offer"))
                        await other_transceiver.receiver.setTrack(track)

        # 我们需要消费掉这个流，即使不转发，否则会导致发送方卡顿
        # MediaBlackhole 会忽略所有收到的数据
        await consumer.start()
        await consumer.addTrack(track)

    @pc.on("icecandidate")
    async def on_icecandidate(candidate):
        """当产生 ICE 候选时，发送给客户端"""
        if candidate:
            await websocket.send_text(json.dumps({
                "type": "candidate",
                "candidate": candidate.to_dict(),
            }))

    @pc.on("connectionstatechange")
    async def on_connectionstatechange():
        """当连接状态改变时"""
        print(f"Connection state of {client_id} changed to {pc.connectionState}")
        if pc.connectionState == "failed":
            await pc.close()
            if pc in rooms[room_id]:
                rooms[room_id].remove(pc)

    try:
        while True:
            # 持续接收来自客户端的 WebSocket 消息
            data = await websocket.receive_text()
            message = json.loads(data)
            
            if message["type"] == "offer":
                # 客户端发送了一个 offer，设置为远程描述
                offer = RTCSessionDescription(sdp=message["sdp"], type=message["type"])
                await pc.setRemoteDescription(offer)

                # 创建 answer
                answer = await pc.createAnswer()
                await pc.setLocalDescription(answer)

                # 将 answer 发送回客户端
                await websocket.send_text(json.dumps({
                    "type": "answer",
                    "sdp": pc.localDescription.sdp,
                }))
            
            elif message["type"] == "candidate":
                # 客户端发送了一个 ICE 候选，添加到本地
                candidate = RTCIceCandidate.from_dict(message["candidate"])
                await pc.addIceCandidate(candidate)

    except WebSocketDisconnect:
        # 客户端断开连接
        print(f"Client {client_id} disconnected from room {room_id}")
        if pc in rooms[room_id]:
            rooms[room_id].remove(pc)
        await pc.close()
        if not rooms[room_id]:
            del rooms[room_id]

if __name__ == "__main__":
    import uvicorn
    # 启动服务器，监听所有网络接口的 8000 端口
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)

步骤 4: 运行 SFU 服务器
在 sfu-server 目录下（确保虚拟环境已激活 (venv)），运行以下命令：
bash
运行

python main.py

你会看到类似输出，表示服务器已成功启动并在 http://0.0.0.0:8000 上监听。
plaintext

INFO:     Uvicorn running on http://0.0.0.0:8000 (Press CTRL+C to quit)
INFO:     Started reloader process [28720] using WatchFiles
INFO:     Started server process [12340]
INFO:     Waiting for application startup.
INFO:     Application startup complete.

第二部分：前端 Vue 3 项目接入
步骤 1: 创建 / 准备 Vue 3 项目
假设你已经有一个 Vue 3 项目。如果没有，可以快速创建一个：
bash
运行

npm create vue@latest
# 按照提示进行配置，创建一个新项目
cd your-vue-project
npm install

步骤 2: 安装 WebSocket 客户端库（可选）
现代浏览器已经内置了 WebSocket API，所以你不一定需要额外的库。但为了代码更简洁健壮，可以使用 vueuse 的 useWebSocket 组合式函数。
bash
运行

npm install @vueuse/core

步骤 3: 创建 RTC 服务和组件
为了更好地组织代码，我们先创建一个 rtcService.js 来封装 WebRTC 和 WebSocket 的核心逻辑。
在 src 目录下创建一个 services 文件夹，然后在里面创建 rtcService.js 文件。
javascript
运行

// src/services/rtcService.js

let pc = null;
let localStream = null;
let socket = null;
let isConnected = false;

// 信令服务器地址 (你的 Python SFU 服务器地址)
const SIGNALING_SERVER_URL = 'ws://localhost:8000';

// 本地媒体流约束
const MEDIA_CONSTRAINTS = {
    video: { width: 640, height: 480 },
    audio: true
};

/**
 * 初始化 RTC 连接
 * @param {string} roomId - 房间 ID
 * @param {string} clientId - 客户端 ID
 * @param {function} onStream - 收到远程流时的回调
 * @param {function} onConnectionStateChange - 连接状态变化时的回调
 */
export async function initRTC(roomId, clientId, onStream, onConnectionStateChange) {
    if (isConnected) {
        console.warn('Already connected.');
        return;
    }

    // 1. 获取本地媒体流
    try {
        localStream = await navigator.mediaDevices.getUserMedia(MEDIA_CONSTRAINTS);
    } catch (error) {
        console.error('Error accessing media devices:', error);
        throw error;
    }

    // 2. 创建 RTCPeerConnection
    pc = new RTCPeerConnection({
        iceServers: [
            // 对于本地网络，通常不需要 STUN/TURN 服务器
            // { urls: 'stun:stun.l.google.com:19302' } 
        ]
    });

    // 3. 添加本地流到 PeerConnection
    localStream.getTracks().forEach(track => {
        pc.addTrack(track, localStream);
    });

    // 4. 监听远程流
    pc.ontrack = (event) => {
        console.log('Received remote track:', event.track.kind);
        onStream(event.streams[0]);
    };

    // 5. 监听 ICE 候选
    pc.onicecandidate = (event) => {
        if (event.candidate && socket && socket.readyState === WebSocket.OPEN) {
            socket.send(JSON.stringify({
                type: 'candidate',
                candidate: event.candidate.toJSON(),
            }));
        }
    };

    // 6. 监听连接状态变化
    pc.onconnectionstatechange = () => {
        console.log('Connection state:', pc.connectionState);
        isConnected = pc.connectionState === 'connected';
        onConnectionStateChange(pc.connectionState);
    };

    // 7. 连接信令服务器
    socket = new WebSocket(`${SIGNALING_SERVER_URL}/ws/${roomId}/${clientId}`);

    socket.onopen = () => {
        console.log('WebSocket connected to signaling server');
        // 连接成功后创建并发送 offer
        createAndSendOffer();
    };

    socket.onmessage = async (event) => {
        const message = JSON.parse(event.data);
        console.log('Received message from server:', message.type);

        switch (message.type) {
            case 'answer':
                await pc.setRemoteDescription(new RTCSessionDescription(message));
                break;
            case 'candidate':
                await pc.addIceCandidate(new RTCIceCandidate(message.candidate));
                break;
        }
    };

    socket.onclose = () => {
        console.log('WebSocket disconnected');
        cleanup();
    };

    socket.onerror = (error) => {
        console.error('WebSocket error:', error);
        cleanup();
    };
}

/**
 * 创建并发送 Offer
 */
async function createAndSendOffer() {
    if (!pc || !socket || socket.readyState !== WebSocket.OPEN) return;

    try {
        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);

        socket.send(JSON.stringify({
            type: 'offer',
            sdp: offer.sdp,
        }));
    } catch (error) {
        console.error('Error creating offer:', error);
    }
}

/**
 * 清理 RTC 资源
 */
export function cleanup() {
    if (pc) {
        pc.close();
        pc = null;
    }
    if (localStream) {
        localStream.getTracks().forEach(track => track.stop());
        localStream = null;
    }
    if (socket) {
        socket.close();
        socket = null;
    }
    isConnected = false;
}

/**
 * 获取本地媒体流
 */
export function getLocalStream() {
    return localStream;
}

/**
 * 检查是否已连接
 */
export function checkIsConnected() {
    return isConnected;
}

创建一个 Vue 组件，例如 src/components/VideoConference.vue。
vue

    <template>
      <div class="conference-container">
        <h2>视频会议 (房间: {{ roomId }})</h2>
        
        <div v-if="!isInitialized">
          <input v-model="roomIdInput" placeholder="输入房间号" />
          <button @click="joinRoom">加入房间</button>
        </div>

        <div v-else>
          <div class="video-wrapper">
            <div class="video-item">
              <h3>本地视频</h3>
              <video ref="localVideoRef" autoplay muted playsinline></video>
            </div>
            
            <div class="video-item" v-for="(stream, index) in remoteStreams" :key="index">
              <h3>远程视频 {{ index + 1 }}</h3>
              <video :srcObject="stream" autoplay playsinline></video>
            </div>
          </div>

          <div class="controls">
            <button @click="leaveRoom" :disabled="!isConnected">离开房间</button>
            <p>连接状态: {{ connectionState }}</p>
          </div>
        </div>
      </div>
    </template>

    <script setup>
    import { ref, onMounted, onUnmounted } from 'vue';
    import { initRTC, cleanup, getLocalStream, checkIsConnected } from '@/services/rtcService';

    // 模板引用
    const localVideoRef = ref(null);

    // 响应式数据
    const roomIdInput = ref('room123');
    const roomId = ref('');
    const remoteStreams = ref([]);
    const isInitialized = ref(false);
    const isConnected = ref(false);
    const connectionState = ref('disconnected');

    // 加入房间
    const joinRoom = async () => {
      if (!roomIdInput.value.trim()) {
        alert('请输入房间号');
        return;
      }

      roomId.value = roomIdInput.value.trim();
      
      try {
        // 生成一个随机的客户端 ID
        const clientId = 'client_' + Math.floor(Math.random() * 10000);
        
        // 初始化 RTC 连接
        await initRTC(
          roomId.value, 
          clientId, 
          handleRemoteStream, 
          handleConnectionStateChange
        );

        isInitialized.value = true;
        isConnected.value = checkIsConnected();
        
        // 将本地流绑定到 video 元素
        const localStream = getLocalStream();
        if (localVideoRef.value && localStream) {
          localVideoRef.value.srcObject = localStream;
        }
      } catch (error) {
        console.error('Failed to join room:', error);
        alert('加入房间失败，请确保已授予媒体权限并检查服务器连接。');
      }
    };

    // 离开房间
    const leaveRoom = () => {
      cleanup();
      remoteStreams.value = [];
      isInitialized.value = false;
      isConnected.value = false;
      connectionState.value = 'disconnected';
      roomId.value = '';
    };

    // 处理收到的远程流
    const handleRemoteStream = (stream) => {
      // 检查是否已存在，避免重复添加
      const exists = remoteStreams.value.some(s => s.id === stream.id);
      if (!exists) {
        remoteStreams.value.push(stream);
      }
    };

    // 处理连接状态变化
    const handleConnectionStateChange = (state) => {
      connectionState.value = state;
      isConnected.value = state === 'connected';
    };

    // 组件卸载时清理资源
    onUnmounted(() => {
      cleanup();
    });
    </script>

    <style scoped>
    .conference-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }
    .video-wrapper {
      display: flex;
      flex-wrap: wrap;
      gap: 20px;
      margin-top: 20px;
    }
    .video-item {
      border: 1px solid #ccc;
      padding: 10px;
      border-radius: 8px;
    }
    video {
      width: 320px;
      height: 240px;
      background-color: #000;
      border-radius: 4px;
    }
    .controls {
      margin-top: 20px;
    }
    button {
      padding: 10px 20px;
      font-size: 16px;
      cursor: pointer;
    }
    button:disabled {
      cursor: not-allowed;
      opacity: 0.5;
    }
    </style>

步骤 4: 在 Vue 应用中使用组件
在你的 src/views/HomeView.vue 或其他页面中引入并使用 VideoConference 组件。
vue

<template>
  <main>
    <h1>欢迎使用本地 SFU 演示</h1>
    <VideoConference />
  </main>
</template>

<script setup>
import VideoConference from '@/components/VideoConference.vue';
</script>

启动你的 Vue 项目：
bash
运行

npm run dev

打开浏览器访问你的 Vue 应用，你应该能看到视频会议界面。输入房间号（例如 room123），点击 “加入房间”，浏览器会请求摄像头和麦克风权限。允许后，你将看到自己的本地视频。
测试多人通话：打开另一个浏览器窗口或标签页，访问同一个地址，输入相同的房间号并加入。你将在两个窗口中都看到对方的视频流，这证明 SFU 服务器正在工作，将一个客户端的流转发给了另一个客户端。
第三部分：与 .NET 8 后端集成（可选）
目前，我们的 SFU 服务器（Python）和前端（Vue）可以独立工作。但在实际项目中，你可能希望 .NET 8 后端来管理房间、用户认证等。
集成思路：

    房间管理：
        前端不再直接连接 Python SFU 的 WebSocket，而是先调用 .NET 8 后端的 API（例如 POST /api/rooms/join）。
        .NET 8 后端验证用户身份、检查房间是否存在 / 创建房间，并将用户加入房间。
        .NET 8 后端生成一个一次性的、有时间限制的令牌（Token），并将其返回给前端。这个令牌可以包含房间 ID、用户 ID 等信息。
    安全连接：
        前端收到令牌后，再连接 Python SFU 的 WebSocket，但连接地址中包含这个令牌，例如：ws://localhost:8000/ws?token=xxx-xxx-xxx。
        Python SFU 服务器在 websocket_endpoint 中收到连接请求时，不再直接信任 URL 中的 room_id 和 client_id。
        相反，它会向 .NET 8 后端发送一个验证请求（例如 GET /api/auth/validate?token=xxx-xxx-xxx）。
        .NET 8 后端验证令牌的有效性，如果有效，则返回房间 ID 和用户 ID。
        Python SFU 服务器根据 .NET 8 返回的信息，再进行后续的房间和连接逻辑。

这种方式下，.NET 8 后端成为了整个系统的 “安全网关” 和 “业务逻辑中心”，而 Python SFU 服务器则专注于媒体流的转发。
总结
至此，你已经成功搭建了一个运行在 Windows 上的、基于 aiortc 的本地 SFU 服务器，并将其与你的 Vue 3 前端项目完美集成。这个方案非常适合用于开发、测试和小型团队的内部演示。
关键点回顾：

    Python 服务器：aiortc 提供了强大的 WebRTC 能力，FastAPI 和 uvicorn 让你能轻松地构建和运行一个高性能的 Web 服务。
    Vue 3 前端：通过浏览器原生的 RTCPeerConnection API 与 SFU 服务器通信，封装 rtcService.js 可以让组件代码更清晰。
    本地网络：在没有公网 IP 和 STUN/TURN 服务器的情况下，此方案在局域网内可以完美运行。
    资源占用：对于 3-5 人的小型会议，资源占用极低。

如果你需要将其部署到生产环境，则需要考虑安全性（HTTPS/WSS）、可扩展性（多 SFU 节点、负载均衡）、以及更强大的媒体处理能力（如使用 MediaSoup 替代 aiortc）。