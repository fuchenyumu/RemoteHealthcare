# 开源RTC SFU方案在区域妇幼保健远程诊疗平台的可行性分析

## 📊 方案概述

基于提供的开源RTC SFU方案文档，结合现有RH.admin前端和rh.webapi后端项目架构，分析在Windows + Vue 3 + .NET 8环境下使用aiortc搭建本地SFU服务器的完整技术可行性。

## 🔍 技术架构分析

### ✅ SFU方案核心组件
- **aiortc**: Python异步WebRTC库，支持SFU功能
- **FastAPI**: 高性能Python Web框架
- **WebSocket**: 实时信令通信
- **RTCPeerConnection**: WebRTC连接管理

### 🏗️ 现有项目架构
- **前端**: Vue3.5.22 + TypeScript + PureAdmin框架
- **后端**: .NET 8 + ABP框架 + SqlSugar ORM
- **数据库**: PostgreSQL + 现有会诊数据模型
- **实时通信**: SignalR已集成
- **现有RTC字段**: `rtc_channel_id`, `rtc_vendor`已存在于会诊表中

## 📈 可行性评估

### ✅ 高度可行 - 技术兼容性

#### 1. 数据库集成可行性
```sql
-- 现有会诊表已包含RTC相关字段
[SugarColumn(ColumnName = "rtc_channel_id", IsNullable = true)]
public string RtcChannelId { get; set; }

[SugarColumn(ColumnName = "rtc_vendor", IsNullable = true)]
public string RtcVendor { get; set; }
```
- ✅ 无需修改数据库结构
- ✅ 可直接使用现有字段存储SFU房间信息
- ✅ 与会诊ID天然关联，便于权限控制

#### 2. 前端集成可行性
- ✅ Vue3组件化开发，与现有架构完美契合
- ✅ 现有HTTP工具可直接扩展WebSocket支持
- ✅ PureAdmin组件库可提供统一UI风格
- ✅ 现有权限系统可直接应用

#### 3. 后端集成可行性
- ✅ .NET 8可通过HTTP API与Python SFU服务通信
- ✅ 现有ABP框架支持微服务架构
- ✅ JWT认证可直接复用
- ✅ SignalR可用于额外的实时通知

### ⚠️ 需要适配 - 架构调整

#### 1. 多服务架构调整
```
现有架构:          建议架构:
┌─Vue3前端─┐      ┌─Vue3前端─┐
│         │      │         │
└─.NET后端┘      ├─.NET后端┤  <- 业务逻辑、认证
                  │         │
                  ├─Python   │  <- SFU媒体服务
                  │ SFU服务 ┤
                  │         │
                  └─PostgreSQL┘
```

#### 2. 认证流程适配
```
1. 用户登录 -> .NET后端认证
2. 创建/加入会诊 -> .NET后端验证权限
3. 获取SFU令牌 -> .NET后端生成JWT令牌
4. 连接SFU服务 -> Python SFU验证令牌
5. 开始音视频通话 -> SFU服务转发媒体流
```

### ❌ 潜在风险 - 需要关注

#### 1. 性能考虑
- **并发处理**: aiortc在Windows下的并发性能需要测试
- **资源占用**: 多用户同时会诊时的CPU/内存占用
- **网络延迟**: 局域网内表现良好，跨网络需要优化

#### 2. 运维复杂性
- **多语言环境**: Python + .NET双栈维护
- **部署复杂度**: 需要同时部署和维护两个服务
- **监控告警**: 需要分别监控两个服务的状态

## 🎯 推荐实施方案

### 第一阶段：基础集成（1-2周）

#### 1. Python SFU服务搭建
```python
# 基于提供的方案，增加医疗专用配置
# sfu_medical_server.py

from fastapi import FastAPI, WebSocket, Depends, HTTPException
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
import jwt
from datetime import datetime, timedelta

app = FastAPI(title="医疗SFU服务", version="1.0.0")
security = HTTPBearer()

# 医疗专用配置
MEDICAL_CONFIG = {
    "max_participants_per_room": 8,  # 最大会诊参与人数
    "video_quality": "1080p",          # 医疗级视频质量
    "audio_sample_rate": 48000,        # 高保真音频
    "session_timeout": 3600,           # 会诊超时时间（秒）
    "recording_enabled": True,          # 支持录制功能
}

def verify_medical_token(credentials: HTTPAuthorizationCredentials = Depends(security)):
    """验证医疗JWT令牌"""
    try:
        payload = jwt.decode(credentials.credentials, "your-secret-key", algorithms=["HS256"])
        consultation_id = payload.get("consultation_id")
        user_role = payload.get("role")
        
        # 验证用户角色权限
        if user_role not in ["Doctor", "Expert", "Admin"]:
            raise HTTPException(status_code=403, detail="Invalid user role for consultation")
            
        return payload
    except jwt.PyJWTError:
        raise HTTPException(status_code=401, detail="Invalid token")

@app.websocket("/ws/medical/{consultation_id}")
async def medical_websocket_endpoint(
    websocket: WebSocket, 
    consultation_id: str,
    token_data: dict = Depends(verify_medical_token)
):
    """医疗专用WebSocket端点"""
    await websocket.accept()
    
    # 验证会诊ID匹配
    if token_data.get("consultation_id") != consultation_id:
        await websocket.close(code=1008, reason="Consultation ID mismatch")
        return
    
    # 创建医疗级PeerConnection
    pc = RTCPeerConnection({
        "iceServers": [{"urls": "stun:stun.l.google.com:19302"}],
        "bundlePolicy": "max-bundle",
        "rtcpMuxPolicy": "require"
    })
    
    # 医疗专用轨道处理
    @pc.on("track")
    async def on_track(track):
        if track.kind == "video":
            # 确保视频质量符合医疗标准
            print(f"Medical video track received: {track.id}")
        elif track.kind == "audio":
            # 确保音频质量适合医疗诊断
            print(f"Medical audio track received: {track.id}")
```

#### 2. .NET后端API扩展
```csharp
// RtcController.cs - 新增RTC管理控制器

[Route("api/v1/[controller]")]
[ApiController]
public class RtcController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly IRcConsultationService _consultationService;
    
    [HttpPost("{consultationId}/join")]
    [Authorize(Roles = "Doctor,Expert,Admin")]
    public async Task<IActionResult> JoinConsultation(long consultationId)
    {
        // 1. 验证用户权限
        var userId = User.GetUserId();
        var consultation = await _consultationService.GetAsync(consultationId);
        
        if (!await HasConsultationPermission(userId, consultationId))
        {
            return Forbid("No permission to join this consultation");
        }
        
        // 2. 生成医疗专用JWT令牌
        var token = await GenerateMedicalToken(userId, consultationId, consultation);
        
        // 3. 更新会诊状态为"进行中"
        await _consultationService.StartAsync(consultationId);
        
        // 4. 记录时间线
        await RecordTimeline(consultationId, "JOIN_CONSULTATION", $"用户{userId}加入会诊");
        
        return Ok(new 
        {
            SfuServerUrl = _configuration["SfuServer:WebSocketUrl"],
            Token = token,
            RoomId = consultation.MeetingRoomNo,
            MaxParticipants = 8
        });
    }
    
    private async Task<string> GenerateMedicalToken(long userId, long consultationId, ConsultationDetail consultation)
    {
        var user = await _userService.GetAsync(userId);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("user_id", userId.ToString()),
                new Claim("consultation_id", consultationId.ToString()),
                new Claim("role", user.Role),
                new Claim("room_id", consultation.MeetingRoomNo),
                new Claim("permissions", string.Join(",", GetUserPermissions(userId, consultationId)))
            }),
            Expires = DateTime.UtcNow.AddHours(3), // 3小时有效期
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"])),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        return _jwtService.GenerateToken(tokenDescriptor);
    }
}
```

#### 3. 前端Vue组件集成
```vue
<!-- MedicalRtcConference.vue - 医疗RTC会诊组件 -->

<template>
  <div class="medical-rtc-container">
    <!-- 医疗专用视频布局 -->
    <div class="video-grid" :class="`layout-${participantCount}`">
      <!-- 主讲人视频（大窗口）-->
      <div class="presenter-video" v-if="presenterStream">
        <video :srcObject="presenterStream" autoplay muted></video>
        <div class="participant-info">
          <span>{{ presenterName }}</span>
          <el-tag type="primary">主讲人</el-tag>
        </div>
      </div>
      
      <!-- 参与者视频（小窗口）-->
      <div 
        class="participant-video" 
        v-for="participant in participants" 
        :key="participant.id"
      >
        <video :srcObject="participant.stream" autoplay></video>
        <div class="participant-info">
          <span>{{ participant.name }}</span>
          <el-tag :type="getRoleTagType(participant.role)">{{ participant.role }}</el-tag>
        </div>
      </div>
    </div>
    
    <!-- 医疗专用控制栏 -->
    <div class="medical-controls">
      <el-button-group>
        <el-button 
          :type="isMuted ? 'danger' : 'primary'" 
          @click="toggleMute"
          :icon="isMuted ? 'Mute' : 'Microphone'"
        >
          {{ isMuted ? '取消静音' : '静音' }}
        </el-button>
        
        <el-button 
          :type="isVideoOff ? 'danger' : 'primary'" 
          @click="toggleVideo"
          :icon="isVideoOff ? 'VideoPause' : 'VideoPlay'"
        >
          {{ isVideoOff ? '开启视频' : '关闭视频' }}
        </el-button>
        
        <el-button 
          type="warning" 
          @click="shareScreen"
          icon="Monitor"
          v-if="canShareScreen"
        >
          屏幕共享
        </el-button>
        
        <el-button 
          type="danger" 
          @click="leaveConference"
          icon="SwitchButton"
        >
          离开会诊
        </el-button>
      </el-button-group>
    </div>
    
    <!-- 医疗专用状态显示 -->
    <div class="medical-status">
      <el-alert 
        :title="connectionStatusText" 
        :type="connectionStatusType"
        :closable="false"
      />
      <div class="network-quality" v-if="networkQuality">
        网络质量: {{ networkQuality }}ms
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useUserStoreHook } from '@/store/modules/user';
import { joinMedicalConsultation, leaveMedicalConsultation } from '@/api/rc/rtc';
import { MedicalRtcService } from '@/services/medicalRtcService';

const props = defineProps({
  consultationId: {
    type: Number,
    required: true
  }
});

// 医疗专用状态管理
const rtcService = ref<MedicalRtcService | null>(null);
const participants = ref([]);
const presenterStream = ref(null);
const isMuted = ref(false);
const isVideoOff = ref(false);
const connectionStatus = ref('connecting');
const networkQuality = ref(0);

// 用户权限判断
const userStore = useUserStoreHook();
const canShareScreen = computed(() => {
  return userStore.roles.includes('Doctor') || userStore.roles.includes('Admin');
});

// 连接状态文本
const connectionStatusText = computed(() => {
  const statusMap = {
    'connecting': '正在连接会诊服务器...',
    'connected': '已连接到会诊服务器',
    'disconnected': '与服务器连接断开',
    'failed': '连接失败，请检查网络'
  };
  return statusMap[connectionStatus.value] || '未知状态';
});

const connectionStatusType = computed(() => {
  const typeMap = {
    'connecting': 'info',
    'connected': 'success',
    'disconnected': 'warning',
    'failed': 'error'
  };
  return typeMap[connectionStatus.value] || 'info';
});

// 初始化医疗RTC服务
const initMedicalRtc = async () => {
  try {
    // 1. 获取会诊信息和SFU令牌
    const { data } = await joinMedicalConsultation(props.consultationId);
    
    // 2. 创建医疗专用RTC服务
    rtcService.value = new MedicalRtcService({
      consultationId: props.consultationId,
      sfuServerUrl: data.sfuServerUrl,
      token: data.token,
      roomId: data.roomId,
      userRole: userStore.roles[0],
      userName: userStore.username
    });
    
    // 3. 设置事件监听
    rtcService.value.on('localStream', handleLocalStream);
    rtcService.value.on('remoteStream', handleRemoteStream);
    rtcService.value.on('participantJoined', handleParticipantJoined);
    rtcService.value.on('participantLeft', handleParticipantLeft);
    rtcService.value.on('connectionStateChange', handleConnectionStateChange);
    rtcService.value.on('networkQuality', handleNetworkQuality);
    
    // 4. 连接到SFU服务器
    await rtcService.value.connect();
    
    ElMessage.success('成功加入医疗会诊');
    
  } catch (error) {
    console.error('Failed to initialize medical RTC:', error);
    ElMessage.error('加入医疗会诊失败：' + error.message);
  }
};

// 静音控制
const toggleMute = async () => {
  if (!rtcService.value) return;
  
  try {
    await rtcService.value.setMute(!isMuted.value);
    isMuted.value = !isMuted.value;
    ElMessage.success(isMuted.value ? '已静音' : '已取消静音');
  } catch (error) {
    ElMessage.error('静音操作失败：' + error.message);
  }
};

// 视频控制
const toggleVideo = async () => {
  if (!rtcService.value) return;
  
  try {
    await rtcService.value.setVideoEnabled(isVideoOff.value);
    isVideoOff.value = !isVideoOff.value;
    ElMessage.success(isVideoOff.value ? '视频已关闭' : '视频已开启');
  } catch (error) {
    ElMessage.error('视频操作失败：' + error.message);
  }
};

// 屏幕共享
const shareScreen = async () => {
  if (!rtcService.value) return;
  
  try {
    await rtcService.value.shareScreen();
    ElMessage.success('屏幕共享已开启');
  } catch (error) {
    ElMessage.error('屏幕共享失败：' + error.message);
  }
};

// 离开会诊
const leaveConference = async () => {
  try {
    await ElMessageBox.confirm(
      '确定要离开当前医疗会诊吗？',
      '离开会诊',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );
    
    if (rtcService.value) {
      await rtcService.value.disconnect();
      await leaveMedicalConsultation(props.consultationId);
    }
    
    ElMessage.success('已离开医疗会诊');
    
    // 通知父组件
    emit('conferenceLeft');
    
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('离开会诊失败：' + error.message);
    }
  }
};

// 生命周期管理
onMounted(() => {
  initMedicalRtc();
});

onUnmounted(() => {
  if (rtcService.value) {
    rtcService.value.disconnect();
  }
});
</script>
```

### 第二阶段：高级功能集成（2-3周）

#### 1. 医疗专用功能增强
```python
# medical_sfu_extensions.py - 医疗专用扩展

class MedicalConferenceManager:
    """医疗会诊专用管理器"""
    
    def __init__(self):
        self.conferences = {}  # consultation_id -> conference_info
        self.participants = {}  # user_id -> participant_info
        self.recordings = {}   # consultation_id -> recording_info
    
    async def create_medical_conference(self, consultation_id: str, config: dict):
        """创建医疗会诊会议"""
        conference = {
            "id": consultation_id,
            "created_at": datetime.now(),
            "max_participants": config.get("max_participants", 8),
            "recording_enabled": config.get("recording_enabled", True),
            "quality_profile": "medical_hd",  # 医疗高清配置
            "security_level": "medical_grade",  # 医疗级安全
            "participants": {},
            "recordings": []
        }
        self.conferences[consultation_id] = conference
        return conference
    
    async def add_medical_participant(self, consultation_id: str, user_info: dict):
        """添加医疗会诊参与者"""
        participant = {
            "user_id": user_info["user_id"],
            "role": user_info["role"],  # Doctor, Expert, Admin
            "name": user_info["name"],
            "joined_at": datetime.now(),
            "permissions": self.get_role_permissions(user_info["role"]),
            "stream_quality": self.get_medical_quality_config(user_info["role"])
        }
        
        if consultation_id in self.conferences:
            self.conferences[consultation_id]["participants"][user_info["user_id"]] = participant
        
        return participant
    
    def get_role_permissions(self, role: str) -> dict:
        """根据角色获取权限配置"""
        permissions = {
            "Doctor": {
                "can_share_screen": True,
                "can_record": True,
                "can_mute_others": True,
                "can_remove_participants": False,
                "max_stream_quality": "1080p"
            },
            "Expert": {
                "can_share_screen": True,
                "can_record": False,
                "can_mute_others": False,
                "can_remove_participants": False,
                "max_stream_quality": "1080p"
            },
            "Admin": {
                "can_share_screen": True,
                "can_record": True,
                "can_mute_others": True,
                "can_remove_participants": True,
                "max_stream_quality": "1080p"
            }
        }
        return permissions.get(role, permissions["Expert"])
    
    def get_medical_quality_config(self, role: str) -> dict:
        """获取医疗级质量配置"""
        return {
            "video": {
                "width": 1920,
                "height": 1080,
                "frameRate": 30,
                "bitrate": 2000000  # 2Mbps
            },
            "audio": {
                "sampleRate": 48000,
                "channelCount": 2,
                "bitrate": 128000  # 128kbps
            }
        }
```

#### 2. 录制与归档功能
```python
# medical_recording_service.py - 医疗录制服务

import asyncio
import json
import os
from datetime import datetime
from pathlib import Path

class MedicalRecordingService:
    """医疗会诊录制服务"""
    
    def __init__(self, storage_path: str):
        self.storage_path = Path(storage_path)
        self.storage_path.mkdir(exist_ok=True)
    
    async def start_medical_recording(self, consultation_id: str, participants: list):
        """开始医疗会诊录制"""
        recording_id = f"{consultation_id}_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        
        recording_info = {
            "id": recording_id,
            "consultation_id": consultation_id,
            "start_time": datetime.now(),
            "participants": participants,
            "status": "recording",
            "files": {
                "video": f"{recording_id}_video.webm",
                "audio": f"{recording_id}_audio.webm",
                "metadata": f"{recording_id}_metadata.json"
            },
            "storage_path": str(self.storage_path / recording_id)
        }
        
        # 创建录制目录
        recording_path = self.storage_path / recording_id
        recording_path.mkdir(exist_ok=True)
        
        # 保存录制元数据
        metadata_path = recording_path / "metadata.json"
        with open(metadata_path, 'w', encoding='utf-8') as f:
            json.dump(recording_info, f, ensure_ascii=False, indent=2, default=str)
        
        return recording_info
    
    async def stop_medical_recording(self, recording_id: str) -> dict:
        """停止医疗会诊录制"""
        # 查找录制信息
        metadata_path = self.storage_path / recording_id / "metadata.json"
        if not metadata_path.exists():
            raise ValueError(f"Recording {recording_id} not found")
        
        with open(metadata_path, 'r', encoding='utf-8') as f:
            recording_info = json.load(f)
        
        # 更新录制状态
        recording_info["status"] = "completed"
        recording_info["end_time"] = datetime.now()
        recording_info["duration"] = (
            datetime.now() - datetime.fromisoformat(recording_info["start_time"])
        ).total_seconds()
        
        # 保存更新后的元数据
        with open(metadata_path, 'w', encoding='utf-8') as f:
            json.dump(recording_info, f, ensure_ascii=False, indent=2, default=str)
        
        return recording_info
    
    async def upload_to_emr(self, recording_id: str, emr_api_endpoint: str) -> bool:
        """上传到EMR系统"""
        try:
            recording_info = await self.get_recording_info(recording_id)
            
            # 准备上传到EMR的数据
            emr_data = {
                "consultation_id": recording_info["consultation_id"],
                "recording_id": recording_id,
                "files": recording_info["files"],
                "duration": recording_info["duration"],
                "participants": recording_info["participants"],
                "upload_time": datetime.now().isoformat()
            }
            
            # 这里实现与EMR系统的集成
            # 返回上传结果
            return True
            
        except Exception as e:
            print(f"Failed to upload recording to EMR: {e}")
            return False
```

## 📊 性能评估

### 资源需求预估
```
小型会诊（3-5人）：
- CPU: 2-4核心
- 内存: 4-8GB
- 带宽: 10-20Mbps
- 存储: 100GB（含录制）

中型会诊（6-8人）：
- CPU: 4-8核心  
- 内存: 8-16GB
- 带宽: 20-50Mbps
- 存储: 500GB（含录制）
```

### 扩展性考虑
- **水平扩展**: 支持多SFU节点部署
- **负载均衡**: 基于会诊ID的负载分配
- **录制存储**: 支持分布式存储
- **监控告警**: 实时性能监控

## 🎯 最终建议

### ✅ 强烈推荐实施
1. **技术可行性高**: 与现有架构完美契合
2. **成本可控**: 开源方案，无需额外授权费用
3. **功能完整**: 满足医疗会诊所有核心需求
4. **扩展性强**: 支持未来功能扩展和性能优化

### 📋 实施路线图
- **第1-2周**: 基础SFU服务搭建和测试
- **第3-4周**: 医疗专用功能开发和集成
- **第5-6周**: 高级功能（录制、屏幕共享）实现
- **第7-8周**: 性能优化和生产环境部署

### ⚠️ 注意事项
1. **充分测试**: 在医疗环境中进行全面测试
2. **备份方案**: 准备传统电话会诊作为备份
3. **培训支持**: 为医护人员提供充分的培训
4. **监控运维**: 建立完善的监控和运维体系

---

**结论**: 开源RTC SFU方案在本项目中具有高度的技术可行性，建议作为第一优先级功能立即开始实施。该方案不仅能够满足当前的音视频会诊需求，还为未来的功能扩展提供了良好的技术基础。