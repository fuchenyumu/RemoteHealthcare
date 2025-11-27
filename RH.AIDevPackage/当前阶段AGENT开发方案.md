# 区域妇幼保健远程诊疗平台 - 当前阶段AGENT开发方案

## 🎯 方案概述

基于现有功能实现优先级方案、开源RTC SFU方案和可行性分析，制定一个能够完全自主运行的AGENT开发方案。该方案聚焦于第一优先级功能，实现从0到1的完整远程会诊闭环系统。

## 📋 核心目标

### 🎯 主要目标
- **实现完整远程会诊闭环**: 申请→审核→音视频会诊→报告→归档
- **支持多方实时音视频**: 省妇幼-市妇幼-本院三方会诊
- **医疗级质量保障**: 1080P高清视频，48kHz高保真音频
- **权限精细化控制**: 基于角色的访问控制（RBAC）
- **数据安全合规**: 患者隐私保护，操作审计日志

### 🏥 业务场景覆盖
1. **远程会诊申请**: 本院医生提交会诊申请
2. **会诊审核排期**: 医务科审核，协调会诊时间
3. **实时音视频会诊**: 多方专家在线讨论
4. **屏幕共享标注**: 病历资料同步查看和标注
5. **会诊报告生成**: 专家意见汇总和电子签名
6. **报告归档存储**: 集成EMR系统，永久保存

## 🏗️ 技术架构设计

### 📐 整体架构
```
┌─────────────────────────────────────────────────────────────┐
│                    前端层 (Vue3 + PureAdmin)               │
├─────────────────────────────────────────────────────────────┤
│  会诊管理  │  音视频会诊  │  患者资料  │  报告生成  │  系统管理  │
├─────────────────────────────────────────────────────────────┤
│                  API网关 (.NET 8 + ABP)                     │
├─────────────────────────────────────────────────────────────┤
│    业务服务层   │    媒体服务层   │    数据服务层   │    基础服务层   │
│ 会诊服务│权限服务│患者服务│Python SFU│文件服务│PostgreSQL│Redis│日志服务│
├─────────────────────────────────────────────────────────────┤
│                    基础设施层                              │
│              Windows Server / Linux                        │
│              Docker容器化部署                               │
└─────────────────────────────────────────────────────────────┘
```

### 🔧 核心技术栈

#### 前端技术栈
```
框架: Vue 3.5.22 + TypeScript 5.9.3
UI库: Element Plus 2.11.4 + PureAdmin 2.6.2
状态管理: Pinia 3.0.3
路由: Vue Router 4.5.1
构建工具: Vite 7.1.9
CSS: TailwindCSS 4.1.14 + Animate.css 4.1.1
图标: Iconify + Lucide React
```

#### 后端技术栈
```
框架: .NET 8 + ABP Framework
ORM: SqlSugar + PostgreSQL
认证: JWT + Identity Server
实时通信: SignalR + WebSocket
API文档: Swagger/OpenAPI
缓存: Redis + Memory Cache
```

#### 媒体服务栈
```
SFU服务器: Python + aiortc + FastAPI
媒体协议: WebRTC (SRTP/DTLS/STUN/TURN)
视频编码: H.264/VP8/VP9
音频编码: Opus/PCMU/PCMA
容器格式: WebM/MP4
```

## 📦 功能模块设计

### 1️⃣ 会诊管理模块 (Consultation Management)

#### 功能清单
```typescript
interface ConsultationModule {
  // 会诊申请
  createConsultation(data: ConsultationApplication): Promise<Consultation>;
  submitApplication(id: number): Promise<void>;
  
  // 会诊审核
  auditConsultation(id: number, audit: AuditDecision): Promise<void>;
  scheduleConsultation(id: number, schedule: ConsultationSchedule): Promise<void>;
  
  // 会诊执行
  startConsultation(id: number): Promise<ConsultationSession>;
  joinConsultation(id: number): Promise<ConferenceCredentials>;
  endConsultation(id: number): Promise<ConsultationReport>;
  
  // 会诊查询
  getConsultationList(query: ConsultationQuery): Promise<PagedResult<Consultation>>;
  getConsultationDetail(id: number): Promise<ConsultationDetail>;
  
  // 状态管理
  updateConsultationStatus(id: number, status: ConsultationStatus): Promise<void>;
  trackConsultationTimeline(id: number): Promise<ConsultationTimeline[]>;
}
```

#### 核心组件
```vue
<!-- ConsultationList.vue -->
<template>
  <div class="consultation-list">
    <ReVxeGrid 
      :columns="columns" 
      :data="consultationList"
      :loading="loading"
      @cell-click="handleCellClick"
    >
      <template #toolbar>
        <el-button type="primary" @click="handleCreate">
          <Icon icon="ep:plus" /> 新建会诊
        </el-button>
        <el-button @click="handleBatchAudit" v-if="canBatchAudit">
          <Icon icon="ep:check" /> 批量审核
        </el-button>
      </template>
      
      <template #status="{ row }">
        <el-tag :type="getStatusType(row.consultationStatus)">
          {{ getStatusText(row.consultationStatus) }}
        </el-tag>
      </template>
      
      <template #actions="{ row }">
        <el-button-group>
          <el-button size="small" @click="handleView(row)">查看</el-button>
          <el-button size="small" @click="handleEdit(row)" v-if="canEdit(row)">编辑</el-button>
          <el-button size="small" @click="handleAudit(row)" v-if="canAudit(row)">审核</el-button>
          <el-button size="small" type="primary" @click="handleJoin(row)" v-if="canJoin(row)">
            加入会诊
          </el-button>
        </el-button-group>
      </template>
    </ReVxeGrid>
  </div>
</template>
```

### 2️⃣ 实时音视频模块 (Real-time Communication)

#### 功能清单
```typescript
interface RealtimeCommunicationModule {
  // 媒体设备管理
  getMediaDevices(): Promise<MediaDeviceInfo[]>;
  requestMediaAccess(constraints: MediaStreamConstraints): Promise<MediaStream>;
  
  // 会议连接
  createConference(roomId: string): Promise<Conference>;
  joinConference(roomId: string, token: string): Promise<ConferenceSession>;
  leaveConference(): Promise<void>;
  
  // 媒体控制
  toggleAudio(): Promise<void>;
  toggleVideo(): Promise<void>;
  shareScreen(): Promise<void>;
  stopScreenShare(): Promise<void>;
  
  // 质量监控
  getNetworkQuality(): Promise<NetworkQuality>;
  getConnectionStats(): Promise<RTCStatsReport>;
  
  // 录制功能
  startRecording(): Promise<string>;
  stopRecording(): Promise<RecordingInfo>;
  getRecordings(): Promise<RecordingInfo[]>;
}
```

#### 核心服务
```typescript
// services/medicalRtcService.ts
export class MedicalRtcService extends EventEmitter {
  private peerConnection: RTCPeerConnection | null = null;
  private localStream: MediaStream | null = null;
  private remoteStreams: Map<string, MediaStream> = new Map();
  private websocket: WebSocket | null = null;
  private configuration: MedicalRtcConfig;
  
  constructor(config: MedicalRtcConfig) {
    super();
    this.configuration = config;
  }
  
  async connect(): Promise<void> {
    try {
      // 1. 获取本地媒体流
      this.localStream = await navigator.mediaDevices.getUserMedia({
        video: {
          width: 1920,
          height: 1080,
          frameRate: 30
        },
        audio: {
          sampleRate: 48000,
          channelCount: 2,
          echoCancellation: true,
          noiseSuppression: true
        }
      });
      
      // 2. 创建PeerConnection
      this.peerConnection = new RTCPeerConnection({
        iceServers: [
          { urls: 'stun:stun.l.google.com:19302' },
          { urls: 'stun:stun1.l.google.com:19302' }
        ],
        bundlePolicy: 'max-bundle',
        rtcpMuxPolicy: 'require'
      });
      
      // 3. 添加本地轨道
      this.localStream.getTracks().forEach(track => {
        this.peerConnection!.addTrack(track, this.localStream!);
      });
      
      // 4. 连接WebSocket
      await this.connectWebSocket();
      
      // 5. 创建并发送offer
      await this.createAndSendOffer();
      
      this.emit('connected');
      
    } catch (error) {
      console.error('Failed to connect medical RTC:', error);
      throw new Error(`医疗RTC连接失败: ${error.message}`);
    }
  }
  
  private async connectWebSocket(): Promise<void> {
    return new Promise((resolve, reject) => {
      const wsUrl = `${this.configuration.sfuServerUrl}/ws/medical/${this.configuration.consultationId}`;
      
      this.websocket = new WebSocket(wsUrl);
      
      this.websocket.onopen = () => {
        console.log('WebSocket connected to medical SFU server');
        resolve();
      };
      
      this.websocket.onmessage = async (event) => {
        const message = JSON.parse(event.data);
        await this.handleSignalingMessage(message);
      };
      
      this.websocket.onerror = (error) => {
        console.error('WebSocket error:', error);
        reject(error);
      };
      
      this.websocket.onclose = () => {
        console.log('WebSocket disconnected from medical SFU server');
        this.emit('disconnected');
      };
    });
  }
  
  private async handleSignalingMessage(message: SignalingMessage): Promise<void> {
    switch (message.type) {
      case 'offer':
        await this.handleOffer(message);
        break;
      case 'answer':
        await this.handleAnswer(message);
        break;
      case 'candidate':
        await this.handleCandidate(message);
        break;
      case 'participant-joined':
        this.handleParticipantJoined(message);
        break;
      case 'participant-left':
        this.handleParticipantLeft(message);
        break;
      default:
        console.warn('Unknown signaling message type:', message.type);
    }
  }
  
  async toggleAudio(): Promise<void> {
    if (!this.localStream) return;
    
    const audioTrack = this.localStream.getAudioTracks()[0];
    if (audioTrack) {
      audioTrack.enabled = !audioTrack.enabled;
      this.emit('audio-toggled', audioTrack.enabled);
    }
  }
  
  async toggleVideo(): Promise<void> {
    if (!this.localStream) return;
    
    const videoTrack = this.localStream.getVideoTracks()[0];
    if (videoTrack) {
      videoTrack.enabled = !videoTrack.enabled;
      this.emit('video-toggled', videoTrack.enabled);
    }
  }
  
  async shareScreen(): Promise<void> {
    try {
      const screenStream = await navigator.mediaDevices.getDisplayMedia({
        video: {
          width: 1920,
          height: 1080,
          frameRate: 30
        },
        audio: true
      });
      
      // 替换视频轨道
      const videoTrack = screenStream.getVideoTracks()[0];
      const sender = this.peerConnection?.getSenders().find(s => 
        s.track && s.track.kind === 'video'
      );
      
      if (sender) {
        await sender.replaceTrack(videoTrack);
      }
      
      this.emit('screen-sharing-started');
      
      // 监听屏幕共享结束
      videoTrack.onended = () => {
        this.stopScreenShare();
      };
      
    } catch (error) {
      console.error('Failed to share screen:', error);
      throw new Error(`屏幕共享失败: ${error.message}`);
    }
  }
  
  async stopScreenShare(): Promise<void> {
    if (!this.localStream) return;
    
    try {
      const videoTrack = this.localStream.getVideoTracks()[0];
      const sender = this.peerConnection?.getSenders().find(s => 
        s.track && s.track.kind === 'video'
      );
      
      if (sender && videoTrack) {
        await sender.replaceTrack(videoTrack);
      }
      
      this.emit('screen-sharing-stopped');
      
    } catch (error) {
      console.error('Failed to stop screen share:', error);
      throw new Error(`停止屏幕共享失败: ${error.message}`);
    }
  }
  
  async disconnect(): Promise<void> {
    // 关闭WebSocket连接
    if (this.websocket) {
      this.websocket.close();
      this.websocket = null;
    }
    
    // 关闭PeerConnection
    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }
    
    // 停止本地媒体流
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = null;
    }
    
    // 清理远程流
    this.remoteStreams.clear();
    
    this.emit('disconnected');
  }
}
```

#### 核心组件
```vue
<!-- MedicalRtcConference.vue -->
<template>
  <div class="medical-rtc-conference">
    <!-- 视频网格布局 -->
    <div class="video-grid" :class="`layout-${participantCount}`">
      <!-- 主讲人视频（大窗口）-->
      <div class="presenter-video" v-if="presenterStream">
        <video 
          ref="presenterVideoRef"
          :srcObject="presenterStream" 
          autoplay 
          muted
          playsinline
        ></video>
        <div class="participant-info">
          <span class="name">{{ presenterName }}</span>
          <el-tag type="primary" size="small">主讲人</el-tag>
        </div>
      </div>
      
      <!-- 参与者视频（小窗口）-->
      <div 
        class="participant-video" 
        v-for="participant in participants" 
        :key="participant.id"
        :class="{ 'speaking': participant.isSpeaking }"
      >
        <video 
          :ref="el => setVideoRef(el, participant.id)"
          :srcObject="participant.stream" 
          autoplay
          playsinline
        ></video>
        <div class="participant-info">
          <span class="name">{{ participant.name }}</span>
          <el-tag :type="getRoleTagType(participant.role)" size="small">
            {{ getRoleText(participant.role) }}
          </el-tag>
        </div>
        <div class="participant-status">
          <el-icon v-if="participant.isMuted" class="status-icon muted">
            <Mute />
          </el-icon>
          <el-icon v-if="participant.isVideoOff" class="status-icon video-off">
            <VideoPause />
          </el-icon>
        </div>
      </div>
    </div>
    
    <!-- 控制栏 -->
    <div class="conference-controls">
      <div class="control-group left">
        <div class="connection-status">
          <el-icon :class="connectionStatusClass">
            <CircleCheck v-if="connectionStatus === 'connected'" />
            <Warning v-else-if="connectionStatus === 'connecting'" />
            <CircleClose v-else />
          </el-icon>
          <span>{{ connectionStatusText }}</span>
        </div>
        <div class="network-quality" v-if="networkQuality">
          <el-icon><DataLine /></el-icon>
          <span>{{ networkQuality }}ms</span>
        </div>
      </div>
      
      <div class="control-group center">
        <el-button 
          circle
          :type="isMuted ? 'danger' : 'primary'" 
          @click="handleToggleAudio"
          :disabled="!isConnected"
        >
          <el-icon>
            <Microphone v-if="!isMuted" />
            <Mute v-else />
          </el-icon>
        </el-button>
        
        <el-button 
          circle
          :type="isVideoOff ? 'danger' : 'primary'" 
          @click="handleToggleVideo"
          :disabled="!isConnected"
        >
          <el-icon>
            <VideoPlay v-if="!isVideoOff" />
            <VideoPause v-else />
          </el-icon>
        </el-button>
        
        <el-button 
          circle
          type="warning"
          @click="handleShareScreen"
          :disabled="!canShareScreen || !isConnected"
          v-if="canShareScreen"
        >
          <el-icon><Monitor /></el-icon>
        </el-button>
        
        <el-button 
          circle
          type="info"
          @click="handleToggleWhiteboard"
          :disabled="!isConnected"
          v-if="canUseWhiteboard"
        >
          <el-icon><EditPen /></el-icon>
        </el-button>
      </div>
      
      <div class="control-group right">
        <el-button 
          type="danger"
          @click="handleLeaveConference"
          :disabled="!isConnected"
        >
          <el-icon><SwitchButton /></el-icon>
          离开会诊
        </el-button>
      </div>
    </div>
    
    <!-- 侧边栏工具 -->
    <div class="sidebar-tools" v-if="showSidebar">
      <el-tabs v-model="activeSidebarTab" class="sidebar-tabs">
        <el-tab-pane name="participants" label="参与者">
          <ParticipantList :participants="participants" />
        </el-tab-pane>
        <el-tab-pane name="chat" label="聊天">
          <ConferenceChat :messages="chatMessages" @send="handleSendMessage" />
        </el-tab-pane>
        <el-tab-pane name="files" label="文件">
          <FileShare :files="sharedFiles" @share="handleShareFile" />
        </el-tab-pane>
        <el-tab-pane name="whiteboard" label="白板" v-if="canUseWhiteboard">
          <Whiteboard :enabled="whiteboardEnabled" @draw="handleWhiteboardDraw" />
        </el-tab-pane>
      </el-tabs>
    </div>
    
    <!-- 状态提示 -->
    <el-alert 
      v-if="showAlert"
      :title="alertMessage"
      :type="alertType"
      :closable="false"
      class="status-alert"
    />
  </div>
</template>
```

### 3️⃣ 患者资料管理模块 (Patient Data Management)

#### 功能清单
```typescript
interface PatientDataModule {
  // 患者信息管理
  getPatientList(query: PatientQuery): Promise<PagedResult<Patient>>;
  getPatientDetail(id: number): Promise<PatientDetail>;
  createPatient(data: PatientCreate): Promise<number>;
  updatePatient(id: number, data: PatientUpdate): Promise<void>;
  
  // 资料包管理
  createPatientPack(patientId: number): Promise<PatientPack>;
  addFileToPack(packId: number, file: File): Promise<void>;
  removeFileFromPack(packId: number, fileId: number): Promise<void>;
  getPackFiles(packId: number): Promise<PatientFile[]>;
  
  // 数据同步
  syncFromHIS(patientId: number): Promise<SyncResult>;
  syncFromLIS(patientId: number): Promise<SyncResult>;
  syncFromPACS(patientId: number): Promise<SyncResult>;
  
  // 隐私保护
  desensitizePatientData(data: PatientData): Promise<DesensitizedPatientData>;
  auditDataAccess(patientId: number, action: string): Promise<void>;
}
```

#### 核心组件
```vue
<!-- PatientDataViewer.vue -->
<template>
  <div class="patient-data-viewer">
    <!-- 患者基本信息 -->
    <el-card class="patient-info-card">
      <template #header>
        <div class="card-header">
          <span>患者基本信息</span>
          <el-button 
            type="primary" 
            size="small"
            @click="handleEditPatient"
            v-if="canEditPatient"
          >
            编辑
          </el-button>
        </div>
      </template>
      
      <el-descriptions :column="3" border>
        <el-descriptions-item label="患者姓名">
          {{ desensitizedPatientData.name }}
        </el-descriptions-item>
        <el-descriptions-item label="性别">
          {{ patientData.gender }}
        </el-descriptions-item>
        <el-descriptions-item label="年龄">
          {{ patientData.age }}岁
        </el-descriptions-item>
        <el-descriptions-item label="病历号">
          {{ patientData.medicalRecordNo }}
        </el-descriptions-item>
        <el-descriptions-item label="联系电话">
          {{ desensitizedPatientData.phone }}
        </el-descriptions-item>
        <el-descriptions-item label="身份证号">
          {{ desensitizedPatientData.idCard }}
        </el-descriptions-item>
      </el-descriptions>
    </el-card>
    
    <!-- 资料包管理 -->
    <el-card class="patient-pack-card">
      <template #header>
        <div class="card-header">
          <span>患者资料包</span>
          <div class="header-actions">
            <el-button 
              type="success" 
              size="small"
              @click="handleSyncData"
              :loading="syncLoading"
            >
              <Icon icon="ep:refresh" /> 同步数据
            </el-button>
            <el-button 
              type="primary" 
              size="small"
              @click="handleUploadFile"
            >
              <Icon icon="ep:upload" /> 上传文件
            </el-button>
          </div>
        </div>
      </template>
      
      <el-tabs v-model="activeTab" class="pack-tabs">
        <el-tab-pane label="病历资料" name="medical-records">
          <FileList 
            :files="medicalRecords" 
            :loading="fileLoading"
            @view="handleViewFile"
            @download="handleDownloadFile"
            @remove="handleRemoveFile"
          />
        </el-tab-pane>
        <el-tab-pane label="检验报告" name="lab-reports">
          <LabReportList 
            :reports="labReports"
            @view="handleViewLabReport"
          />
        </el-tab-pane>
        <el-tab-pane label="影像资料" name="imaging-data">
          <ImagingDataList 
            :images="imagingData"
            @view="handleViewImage"
          />
        </el-tab-pane>
        <el-tab-pane label="其他资料" name="other-files">
          <FileList 
            :files="otherFiles"
            @view="handleViewFile"
            @download="handleDownloadFile"
            @remove="handleRemoveFile"
          />
        </el-tab-pane>
      </el-tabs>
    </el-card>
    
    <!-- 数据同步日志 -->
    <el-card class="sync-log-card" v-if="showSyncLog">
      <template #header>
        <span>数据同步日志</span>
      </template>
      <SyncLogList :logs="syncLogs" />
    </el-card>
  </div>
</template>
```

### 4️⃣ 系统管理模块 (System Management)

#### 功能清单
```typescript
interface SystemManagementModule {
  // 用户管理
  getUserList(query: UserQuery): Promise<PagedResult<User>>;
  createUser(data: UserCreate): Promise<number>;
  updateUser(id: number, data: UserUpdate): Promise<void>;
  deleteUser(id: number): Promise<void>;
  assignRoles(userId: number, roleIds: number[]): Promise<void>;
  
  // 角色权限管理
  getRoleList(): Promise<Role[]>;
  createRole(data: RoleCreate): Promise<number>;
  updateRolePermissions(roleId: number, permissions: string[]): Promise<void>;
  
  // 系统配置
  getSystemConfig(): Promise<SystemConfig>;
  updateSystemConfig(config: SystemConfig): Promise<void>;
  
  // 操作日志
  getOperationLogs(query: LogQuery): Promise<PagedResult<OperationLog>>;
  exportOperationLogs(query: LogQuery): Promise<Blob>;
  
  // 系统监控
  getSystemStatus(): Promise<SystemStatus>;
  getRealTimeStats(): Promise<RealTimeStats>;
}
```

## 🗄️ 数据库设计

### 📊 核心数据表结构

#### 会诊主表 (PUREST_RC_CONSULTATION)
```sql
-- 远程会诊主表
CREATE TABLE PUREST_RC_CONSULTATION (
    ID BIGSERIAL PRIMARY KEY,
    CASE_ID BIGINT NOT NULL,                                    -- 患者快照ID
    PACK_ID BIGINT,                                            -- 资料包ID
    APPLY_ORG_ID BIGINT NOT NULL,                             -- 申请机构ID
    APPLY_DOCTOR_ID BIGINT NOT NULL,                           -- 申请医生ID
    TARGET_ORG_ID BIGINT,                                      -- 目标机构ID
    TARGET_DEPARTMENT VARCHAR(100),                           -- 目标科室
    TARGET_EXPERT_ID BIGINT,                                   -- 目标专家ID
    PURPOSE TEXT,                                              -- 会诊目的
    EMERGENCY_LEVEL VARCHAR(20) NOT NULL,                      -- 紧急程度
    CONSULTATION_STATUS VARCHAR(30) NOT NULL,                  -- 会诊状态
    DESIRED_START_TIME TIMESTAMP,                             -- 期望开始时间
    DESIRED_END_TIME TIMESTAMP,                               -- 期望结束时间
    SCHEDULED_START_TIME TIMESTAMP,                           --  scheduled开始时间
    SCHEDULED_END_TIME TIMESTAMP,                             --  scheduled结束时间
    MEETING_ROOM_NO VARCHAR(50),                              -- 会议室编号
    RTC_CHANNEL_ID VARCHAR(100),                              -- RTC频道ID
    RTC_VENDOR VARCHAR(50),                                   -- RTC服务商
    AUDIT_DOCTOR_ID BIGINT,                                   -- 审核医生ID
    AUDIT_TIME TIMESTAMP,                                     -- 审核时间
    AUDIT_COMMENT TEXT,                                       -- 审核意见
    CLOSE_REASON TEXT,                                        -- 关闭原因
    CREATED_BY BIGINT NOT NULL,                               -- 创建人
    CREATED_TIME TIMESTAMP DEFAULT CURRENT_TIMESTAMP,         -- 创建时间
    UPDATED_BY BIGINT,                                        -- 更新人
    UPDATED_TIME TIMESTAMP,                                   -- 更新时间
    IS_DELETED BOOLEAN DEFAULT FALSE,                         -- 删除标记
    VERSION INT DEFAULT 1                                     -- 版本号
);

-- 索引
CREATE INDEX IDX_CONSULTATION_STATUS ON PUREST_RC_CONSULTATION(CONSULTATION_STATUS);
CREATE INDEX IDX_CONSULTATION_APPLY_DOCTOR ON PUREST_RC_CONSULTATION(APPLY_DOCTOR_ID);
CREATE INDEX IDX_CONSULTATION_TARGET_ORG ON PUREST_RC_CONSULTATION(TARGET_ORG_ID);
CREATE INDEX IDX_CONSULTATION_CREATE_TIME ON PUREST_RC_CONSULTATION(CREATED_TIME);
```

#### 会诊成员表 (PUREST_RC_CONSULTATION_MEMBER)
```sql
-- 会诊成员表
CREATE TABLE PUREST_RC_CONSULTATION_MEMBER (
    ID BIGSERIAL PRIMARY KEY,
    CONSULTATION_ID BIGINT NOT NULL,                          -- 会诊ID
    USER_ID BIGINT NOT NULL,                                   -- 用户ID
    ROLE VARCHAR(30) NOT NULL,                                -- 角色 (HOST/EXPERT/OBSERVER)
    JOIN_STATUS VARCHAR(20) NOT NULL,                        -- 加入状态
    JOIN_TIME TIMESTAMP,                                       -- 加入时间
    LEAVE_TIME TIMESTAMP,                                      -- 离开时间
    PERMISSIONS JSONB,                                        -- 权限配置
    RTC_PEER_ID VARCHAR(100),                                 -- RTC对端ID
    CREATED_TIME TIMESTAMP DEFAULT CURRENT_TIMESTAMP,         -- 创建时间
    FOREIGN KEY (CONSULTATION_ID) REFERENCES PUREST_RC_CONSULTATION(ID)
);

CREATE INDEX IDX_MEMBER_CONSULTATION ON PUREST_RC_CONSULTATION_MEMBER(CONSULTATION_ID);
CREATE INDEX IDX_MEMBER_USER ON PUREST_RC_CONSULTATION_MEMBER(USER_ID);
```

#### 会诊附件表 (PUREST_RC_CONSULTATION_ATTACHMENT)
```sql
-- 会诊附件表
CREATE TABLE PUREST_RC_CONSULTATION_ATTACHMENT (
    ID BIGSERIAL PRIMARY KEY,
    CONSULTATION_ID BIGINT NOT NULL,                          -- 会诊ID
    FILE_NAME VARCHAR(255) NOT NULL,                          -- 文件名称
    FILE_PATH VARCHAR(500),                                   -- 文件路径
    FILE_SIZE BIGINT,                                         -- 文件大小
    FILE_TYPE VARCHAR(100),                                   -- 文件类型
    SOURCE_SYSTEM VARCHAR(50),                                -- 来源系统 (HIS/LIS/PACS)
    ATTACHMENT_TYPE VARCHAR(50),                              -- 附件类型
    UPLOAD_TIME TIMESTAMP DEFAULT CURRENT_TIMESTAMP,          -- 上传时间
    UPLOADED_BY BIGINT NOT NULL,                              -- 上传人
    IS_DELETED BOOLEAN DEFAULT FALSE,                         -- 删除标记
    FOREIGN KEY (CONSULTATION_ID) REFERENCES PUREST_RC_CONSULTATION(ID)
);

CREATE INDEX IDX_ATTACHMENT_CONSULTATION ON PUREST_RC_CONSULTATION_ATTACHMENT(CONSULTATION_ID);
CREATE INDEX IDX_ATTACHMENT_TYPE ON PUREST_RC_CONSULTATION_ATTACHMENT(ATTACHMENT_TYPE);
```

#### 会诊报告表 (PUREST_RC_CONSULTATION_REPORT)
```sql
-- 会诊报告表
CREATE TABLE PUREST_RC_CONSULTATION_REPORT (
    ID BIGSERIAL PRIMARY KEY,
    CONSULTATION_ID BIGINT NOT NULL,                          -- 会诊ID
    REPORT_NO VARCHAR(100) UNIQUE NOT NULL,                   -- 报告编号
    SUMMARY TEXT,                                              -- 会诊总结
    DIAGNOSIS TEXT,                                            -- 诊断意见
    TREATMENT_SUGGESTION TEXT,                                -- 治疗建议
    MEDICATION_SUGGESTION TEXT,                               -- 用药建议
    FOLLOW_UP_SUGGESTION TEXT,                                -- 随访建议
    EXPERT_OPINIONS JSONB,                                    -- 专家意见汇总
    REPORT_STATUS VARCHAR(30) NOT NULL,                      -- 报告状态
    CREATED_BY BIGINT NOT NULL,                               -- 创建人
    CREATED_TIME TIMESTAMP DEFAULT CURRENT_TIMESTAMP,         -- 创建时间
    SUBMITTED_BY BIGINT,                                      -- 提交人
    SUBMITTED_TIME TIMESTAMP,                                 -- 提交时间
    SIGNED_BY BIGINT,                                          -- 签署人
    SIGNED_TIME TIMESTAMP,                                    -- 签署时间
    SIGNATURE_DATA TEXT,                                      -- 签名数据
    IS_DELETED BOOLEAN DEFAULT FALSE,                         -- 删除标记
    FOREIGN KEY (CONSULTATION_ID) REFERENCES PUREST_RC_CONSULTATION(ID)
);

CREATE INDEX IDX_REPORT_CONSULTATION ON PUREST_RC_CONSULTATION_REPORT(CONSULTATION_ID);
CREATE INDEX IDX_REPORT_STATUS ON PUREST_RC_CONSULTATION_REPORT(REPORT_STATUS);
CREATE INDEX IDX_REPORT_NO ON PUREST_RC_CONSULTATION_REPORT(REPORT_NO);
```

#### 患者快照表 (PUREST_RC_PATIENT_CASE)
```sql
-- 患者快照表
CREATE TABLE PUREST_RC_PATIENT_CASE (
    ID BIGSERIAL PRIMARY KEY,
    PATIENT_NAME VARCHAR(100) NOT NULL,                       -- 患者姓名
    PATIENT_TYPE VARCHAR(20) NOT NULL,                        -- 患者类型 (成人/儿童/孕妇)
    GENDER VARCHAR(10),                                       -- 性别
    AGE INT,                                                   -- 年龄
    DATE_OF_BIRTH DATE,                                       -- 出生日期
    MEDICAL_RECORD_NO VARCHAR(100),                           -- 病历号
    ID_CARD_NO VARCHAR(50),                                   -- 身份证号
    PHONE_NUMBER VARCHAR(50),                                 -- 联系电话
    PREGNANCY_WEEKS INT,                                      -- 孕周
    CHILD_MONTH_AGE INT,                                      -- 儿童月龄
    CHIEF_COMPLAINT TEXT,                                       -- 主诉
    HISTORY_PRESENT_ILLNESS TEXT,                           -- 现病史
    PAST_HISTORY TEXT,                                        -- 既往史
    PERSONAL_HISTORY TEXT,                                    -- 个人史
    FAMILY_HISTORY TEXT,                                      -- 家族史
    PHYSICAL_EXAMINATION TEXT,                                -- 体格检查
    AUXILIARY_EXAMINATION TEXT,                               -- 辅助检查
    PRELIMINARY_DIAGNOSIS TEXT,                               -- 初步诊断
    DIFFERENTIAL_DIAGNOSIS TEXT,                              -- 鉴别诊断
    TREATMENT_PLAN TEXT,                                      -- 治疗计划
    SOURCE_SYSTEM VARCHAR(50),                                -- 来源系统
    SYNC_STATUS VARCHAR(30),                                  -- 同步状态
    SYNC_TIME TIMESTAMP,                                       -- 同步时间
    CREATED_BY BIGINT NOT NULL,                               -- 创建人
    CREATED_TIME TIMESTAMP DEFAULT CURRENT_TIMESTAMP,         -- 创建时间
    UPDATED_BY BIGINT,                                        -- 更新人
    UPDATED_TIME TIMESTAMP,                                   -- 更新时间
    IS_DELETED BOOLEAN DEFAULT FALSE                          -- 删除标记
);

CREATE INDEX IDX_PATIENT_CASE_NAME ON PUREST_RC_PATIENT_CASE(PATIENT_NAME);
CREATE INDEX IDX_PATIENT_CASE_MEDICAL_NO ON PUREST_RC_PATIENT_CASE(MEDICAL_RECORD_NO);
CREATE INDEX IDX_PATIENT_CASE_SOURCE ON PUREST_RC_PATIENT_CASE(SOURCE_SYSTEM);
```

## 🚀 开发实施计划

### 📅 第一阶段：基础架构搭建 (Week 1-2)

#### Week 1: 项目初始化与环境搭建
```
Day 1-2: 项目初始化
□ 创建Vue3前端项目 (PureAdmin模板)
□ 创建.NET 8后端项目 (ABP框架)
□ 配置开发环境 (VS Code, Visual Studio)
□ 配置代码规范 (ESLint, Prettier, StyleLint)

Day 3-4: 基础架构搭建
□ 配置前端路由和权限系统
□ 搭建后端API框架和数据库连接
□ 配置Swagger文档
□ 实现基础用户认证和授权

Day 5: 数据库设计实现
□ 创建核心数据表结构
□ 实现数据访问层 (Repository Pattern)
□ 配置数据库迁移脚本
□ 创建基础数据种子
```

#### Week 2: 核心功能开发
```
Day 1-2: 会诊管理功能
□ 实现会诊申请功能
□ 实现会诊审核流程
□ 创建会诊列表和详情页面
□ 实现会诊状态管理

Day 3-4: 患者资料管理
□ 实现患者信息录入
□ 创建患者资料包管理
□ 实现文件上传下载功能
□ 集成数据脱敏功能

Day 5: 系统集成测试
□ 前后端接口联调
□ 基础功能测试
□ 性能优化和bug修复
□ 代码审查和文档更新
```

### 📅 第二阶段：实时音视频功能 (Week 3-4)

#### Week 3: SFU服务器搭建
```
Day 1-2: Python SFU服务开发
□ 搭建aiortc + FastAPI环境
□ 实现基础WebRTC信令服务器
□ 实现SFU媒体转发功能
□ 集成JWT认证机制

Day 3-4: 医疗专用功能开发
□ 实现医疗级音视频质量控制
□ 开发录制和存储功能
□ 实现屏幕共享功能
□ 集成网络质量监控

Day 5: 安全性和稳定性优化
□ 实现医疗数据加密传输
□ 添加连接状态监控
□ 实现断线重连机制
□ 性能调优和压力测试
```

#### Week 4: 前端音视频集成
```
Day 1-2: 前端RTC组件开发
□ 创建医疗RTC服务类
□ 实现音视频采集和渲染
□ 开发会议控制界面
□ 实现媒体设备管理

Day 3-4: 高级功能集成
□ 实现屏幕共享功能
□ 开发白板标注工具
□ 集成聊天和文件共享
□ 实现录制控制界面

Day 5: 音视频功能测试
□ 多方音视频通话测试
□ 网络适应性测试
□ 浏览器兼容性测试
□ 用户体验优化
```

### 📅 第三阶段：高级功能完善 (Week 5-6)

#### Week 5: 报告和工作流功能
```
Day 1-2: 会诊报告功能
□ 实现报告模板管理
□ 创建报告编辑界面
□ 实现专家意见汇总
□ 集成电子签名功能

Day 3-4: 工作流引擎集成
□ 配置LogicFlow工作流
□ 实现会诊流程自动化
□ 开发任务提醒机制
□ 集成消息通知系统

Day 5: 数据分析和报表
□ 实现会诊统计功能
□ 创建数据可视化图表
□ 开发运营分析报表
□ 实现数据导出功能
```

#### Week 6: 系统管理和监控
```
Day 1-2: 系统管理功能
□ 实现用户和角色管理
□ 创建权限配置界面
□ 实现系统配置管理
□ 开发操作日志功能

Day 3-4: 系统监控和运维
□ 实现系统状态监控
□ 创建性能指标仪表板
□ 集成错误日志收集
□ 实现健康检查接口

Day 5: 集成测试和优化
□ 全系统功能测试
□ 性能压力测试
□ 安全性测试
□ 用户体验优化
```

### 📅 第四阶段：部署和上线 (Week 7-8)

#### Week 7: 部署环境准备
```
Day 1-2: 生产环境搭建
□ 配置生产服务器环境
□ 搭建Docker容器化平台
□ 配置负载均衡和反向代理
□ 设置数据库集群和备份

Day 3-4: 部署脚本开发
□ 编写自动化部署脚本
□ 配置CI/CD流水线
□ 实现蓝绿部署方案
□ 设置监控告警系统

Day 5: 预生产环境部署
□ 部署到预生产环境
□ 进行生产环境模拟测试
□ 验证数据迁移方案
□ 性能基准测试
```

#### Week 8: 正式上线和运维
```
Day 1-2: 生产环境部署
□ 正式环境应用部署
□ 数据库初始化和数据迁移
□ 配置域名和SSL证书
□ 设置CDN和静态资源优化

Day 3-4: 上线验证和监控
□ 上线后功能验证测试
□ 实时监控数据收集
□ 用户行为数据分析
□ 性能指标持续监控

Day 5: 文档交付和培训
□ 编写系统操作手册
□ 制作用户使用指南
□ 开展管理员培训
□ 建立技术支持体系
```

## 🔧 技术实现细节

### 🏥 医疗专用功能实现

#### 1. 患者隐私保护
```typescript
// utils/privacy.ts
export class PrivacyProtector {
  /**
   * 患者姓名脱敏
   * 张小明 -> 张*明
   */
  static desensitizeName(name: string): string {
    if (!name || name.length < 2) return name;
    
    if (name.length === 2) {
      return name[0] + '*';
    }
    
    return name[0] + '*'.repeat(name.length - 2) + name[name.length - 1];
  }
  
  /**
   * 手机号脱敏
   * 13812345678 -> 138****5678
   */
  static desensitizePhone(phone: string): string {
    if (!phone || phone.length !== 11) return phone;
    
    return phone.substring(0, 3) + '****' + phone.substring(7);
  }
  
  /**
   * 身份证号脱敏
   * 110101199001011234 -> 110101********1234
   */
  static desensitizeIdCard(idCard: string): string {
    if (!idCard || idCard.length !== 18) return idCard;
    
    return idCard.substring(0, 6) + '********' + idCard.substring(14);
  }
  
  /**
   * 根据用户权限返回相应的脱敏数据
   */
  static desensitizePatientData(data: PatientData, userRole: string): DesensitizedPatientData {
    const roleConfig = {
      'Doctor': {
        nameLevel: 'partial',     // 张*明
        phoneLevel: 'partial',      // 138****5678
        idCardLevel: 'partial',     // 110101********1234
        addressLevel: 'partial'     // 北京市****区
      },
      'Nurse': {
        nameLevel: 'full',        // 张**
        phoneLevel: 'full',       // 138****5678
        idCardLevel: 'full',      // ************1234
        addressLevel: 'full'       // 北京市******
      },
      'Admin': {
        nameLevel: 'none',        // 张小明
        phoneLevel: 'none',       // 13812345678
        idCardLevel: 'none',      // 110101199001011234
        addressLevel: 'none'      // 北京市朝阳区
      }
    };
    
    const config = roleConfig[userRole] || roleConfig['Nurse'];
    
    return {
      id: data.id,
      name: this.desensitizeByLevel(data.name, config.nameLevel),
      phone: this.desensitizeByLevel(data.phone, config.phoneLevel),
      idCard: this.desensitizeByLevel(data.idCard, config.idCardLevel),
      address: this.desensitizeByLevel(data.address, config.addressLevel),
      gender: data.gender,
      age: data.age,
      medicalRecordNo: data.medicalRecordNo,
      // 其他字段根据需求脱敏
    };
  }
}
```

#### 2. 医疗数据标准对接
```csharp
// Services/MedicalDataStandardService.cs
public class MedicalDataStandardService : IMedicalDataStandardService
{
    private readonly ILogger<MedicalDataStandardService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public MedicalDataStandardService(
        ILogger<MedicalDataStandardService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }
    
    /// <summary>
    /// 将患者数据转换为HL7格式
    /// </summary>
    public async Task<string> ConvertToHL7(PatientData patientData)
    {
        try
        {
            var hl7Message = new HL7Message();
            
            // MSH段 - 消息头
            hl7Message.AddSegment(new MSHSegment
            {
                FieldSeparator = "|",
                EncodingCharacters = "^~\\&",
                MessageType = "ADT^A08",
                MessageControlId = Guid.NewGuid().ToString(),
                ProcessingId = "P",
                VersionId = "2.5"
            });
            
            // PID段 - 患者身份信息
            hl7Message.AddSegment(new PIDSegment
            {
                PatientId = patientData.MedicalRecordNo,
                PatientName = new PersonName
                {
                    FamilyName = patientData.Name.Split(' ')[0],
                    GivenName = patientData.Name.Split(' ').Length > 1 ? patientData.Name.Split(' ')[1] : ""
                },
                DateOfBirth = patientData.DateOfBirth,
                Gender = patientData.Gender,
                PhoneNumber = patientData.Phone,
                IdCardNumber = patientData.IdCardNo
            });
            
            // PV1段 - 患者就诊信息
            hl7Message.AddSegment(new PV1Segment
            {
                PatientClass = "I", // 住院
                AdmissionType = "A", // 普通入院
                HospitalService = "MED", // 内科
                PatientType = patientData.PatientType
            });
            
            return hl7Message.ToString();
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert patient data to HL7 format");
            throw new Exception("HL7格式转换失败", ex);
        }
    }
    
    /// <summary>
    /// 将检验数据转换为HL7格式
    /// </summary>
    public async Task<string> ConvertLabReportToHL7(LabReportData labReport)
    {
        try
        {
            var hl7Message = new HL7Message();
            
            // OBR段 - 观察报告段
            hl7Message.AddSegment(new OBRSegment
            {
                ObservationId = labReport.TestCode,
                ObservationName = labReport.TestName,
                ObservationResult = labReport.Result,
                Units = labReport.Units,
                ReferenceRange = labReport.ReferenceRange,
                AbnormalFlags = labReport.AbnormalFlags,
                ObservationDateTime = labReport.TestDate
            });
            
            // OBX段 - 观察结果段
            foreach (var result in labReport.Results)
            {
                hl7Message.AddSegment(new OBXSegment
                {
                    ObservationId = result.ItemCode,
                    ObservationValue = result.Value,
                    Units = result.Units,
                    ReferenceRange = result.ReferenceRange,
                    AbnormalFlags = result.IsAbnormal ? "A" : "N"
                });
            }
            
            return hl7Message.ToString();
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert lab report to HL7 format");
            throw new Exception("检验报告HL7格式转换失败", ex);
        }
    }
    
    /// <summary>
    /// 将影像数据转换为DICOM格式
    /// </summary>
    public async Task<DicomDataset> ConvertToDICOM(ImagingData imagingData)
    {
        try
        {
            var dataset = new DicomDataset();
            
            // 患者信息
            dataset.Add(DicomTag.PatientID, imagingData.PatientId);
            dataset.Add(DicomTag.PatientName, imagingData.PatientName);
            dataset.Add(DicomTag.PatientBirthDate, imagingData.PatientBirthDate);
            dataset.Add(DicomTag.PatientSex, imagingData.PatientGender);
            
            // 检查信息
            dataset.Add(DicomTag.StudyInstanceUID, imagingData.StudyInstanceUID);
            dataset.Add(DicomTag.SeriesInstanceUID, imagingData.SeriesInstanceUID);
            dataset.Add(DicomTag.SOPInstanceUID, imagingData.SOPInstanceUID);
            dataset.Add(DicomTag.Modality, imagingData.Modality);
            dataset.Add(DicomTag.StudyDate, imagingData.StudyDate);
            dataset.Add(DicomTag.StudyTime, imagingData.StudyTime);
            
            // 影像信息
            dataset.Add(DicomTag.Rows, imagingData.Rows);
            dataset.Add(DicomTag.Columns, imagingData.Columns);
            dataset.Add(DicomTag.BitsAllocated, imagingData.BitsAllocated);
            dataset.Add(DicomTag.BitsStored, imagingData.BitsStored);
            dataset.Add(DicomTag.HighBit, imagingData.HighBit);
            dataset.Add(DicomTag.PixelRepresentation, imagingData.PixelRepresentation);
            
            // 影像数据
            dataset.Add(DicomTag.PixelData, imagingData.PixelData);
            
            return dataset;
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert imaging data to DICOM format");
            throw new Exception("影像数据DICOM格式转换失败", ex);
        }
    }
}
```

#### 3. 实时音视频质量保障
```typescript
// services/qualityMonitor.ts
export class QualityMonitor extends EventEmitter {
  private peerConnection: RTCPeerConnection;
  private monitoringInterval: number | null = null;
  private statsHistory: RTCStatsReport[] = [];
  private qualityThresholds = {
    video: {
      bitrate: { min: 500000, target: 1500000, max: 3000000 }, // bps
      framerate: { min: 15, target: 30, max: 60 }, // fps
      resolution: { width: 1280, height: 720 }, // min resolution
      packetLoss: { max: 0.02 }, // 2%
      jitter: { max: 0.03 } // 30ms
    },
    audio: {
      bitrate: { min: 32000, target: 64000, max: 128000 }, // bps
      packetLoss: { max: 0.01 }, // 1%
      jitter: { max: 0.02 } // 20ms
    }
  };
  
  constructor(peerConnection: RTCPeerConnection) {
    super();
    this.peerConnection = peerConnection;
  }
  
  startMonitoring(intervalMs: number = 1000): void {
    if (this.monitoringInterval) {
      this.stopMonitoring();
    }
    
    this.monitoringInterval = window.setInterval(async () => {
      await this.collectStats();
      this.analyzeQuality();
    }, intervalMs);
  }
  
  stopMonitoring(): void {
    if (this.monitoringInterval) {
      window.clearInterval(this.monitoringInterval);
      this.monitoringInterval = null;
    }
  }
  
  private async collectStats(): Promise<void> {
    try {
      const stats = await this.peerConnection.getStats();
      this.statsHistory.push(stats);
      
      // 保持历史记录在合理范围内
      if (this.statsHistory.length > 60) { // 保留1分钟的历史数据
        this.statsHistory.shift();
      }
      
    } catch (error) {
      console.error('Failed to collect stats:', error);
    }
  }
  
  private analyzeQuality(): void {
    if (this.statsHistory.length === 0) return;
    
    const currentStats = this.statsHistory[this.statsHistory.length - 1];
    const qualityReport = this.generateQualityReport(currentStats);
    
    this.emit('quality-report', qualityReport);
    
    // 检测质量下降
    if (qualityReport.overallQuality === 'poor') {
      this.emit('quality-degraded', qualityReport);
      this.suggestQualityImprovement(qualityReport);
    }
    
    // 检测质量恢复
    if (qualityReport.overallQuality === 'good' && 
        this.previousQuality === 'poor') {
      this.emit('quality-recovered', qualityReport);
    }
    
    this.previousQuality = qualityReport.overallQuality;
  }
  
  private generateQualityReport(stats: RTCStatsReport): QualityReport {
    const report: QualityReport = {
      timestamp: Date.now(),
      overallQuality: 'good',
      videoQuality: 'good',
      audioQuality: 'good',
      networkQuality: 'good',
      stats: {}
    };
    
    // 分析视频流质量
    const videoStats = this.analyzeVideoStats(stats);
    report.videoQuality = videoStats.quality;
    report.stats.video = videoStats;
    
    // 分析音频流质量
    const audioStats = this.analyzeAudioStats(stats);
    report.audioQuality = audioStats.quality;
    report.stats.audio = audioStats;
    
    // 分析网络质量
    const networkStats = this.analyzeNetworkStats(stats);
    report.networkQuality = networkStats.quality;
    report.stats.network = networkStats;
    
    // 综合质量评估
    const qualities = [report.videoQuality, report.audioQuality, report.networkQuality];
    const poorCount = qualities.filter(q => q === 'poor').length;
    const fairCount = qualities.filter(q => q === 'fair').length;
    
    if (poorCount >= 2) {
      report.overallQuality = 'poor';
    } else if (poorCount >= 1 || fairCount >= 2) {
      report.overallQuality = 'fair';
    } else {
      report.overallQuality = 'good';
    }
    
    return report;
  }
  
  private analyzeVideoStats(stats: RTCStatsReport): VideoQualityStats {
    const videoStats: VideoQualityStats = {
      quality: 'good',
      bitrate: 0,
      framerate: 0,
      resolution: { width: 0, height: 0 },
      packetLoss: 0,
      jitter: 0
    };
    
    stats.forEach(report => {
      if (report.type === 'inbound-rtp' && report.mediaType === 'video') {
        videoStats.bitrate = report.bytesReceived * 8 / report.timestamp; // bps
        videoStats.framerate = report.framesPerSecond || 0;
        videoStats.resolution = { 
          width: report.frameWidth || 0, 
          height: report.frameHeight || 0 
        };
        videoStats.packetLoss = report.packetsLost / report.packetsReceived;
        videoStats.jitter = report.jitterBuffered || 0;
      }
    });
    
    // 质量评估
    const thresholds = this.qualityThresholds.video;
    
    if (videoStats.packetLoss > thresholds.packetLoss.max ||
        videoStats.jitter > thresholds.jitter.max ||
        videoStats.bitrate < thresholds.bitrate.min ||
        videoStats.framerate < thresholds.framerate.min) {
      videoStats.quality = 'poor';
    } else if (videoStats.packetLoss > thresholds.packetLoss.max * 0.5 ||
               videoStats.jitter > thresholds.jitter.max * 0.5 ||
               videoStats.bitrate < thresholds.bitrate.target * 0.7) {
      videoStats.quality = 'fair';
    } else {
      videoStats.quality = 'good';
    }
    
    return videoStats;
  }
  
  private suggestQualityImprovement(qualityReport: QualityReport): void {
    const suggestions: string[] = [];
    
    // 网络质量建议
    if (qualityReport.networkQuality === 'poor') {
      suggestions.push('网络质量较差，建议检查网络连接');
      suggestions.push('建议关闭其他占用带宽的应用程序');
      suggestions.push('建议切换到更稳定的网络环境');
    }
    
    // 视频质量建议
    if (qualityReport.videoQuality === 'poor') {
      suggestions.push('视频质量较差，建议降低视频分辨率');
      suggestions.push('建议关闭高清视频模式');
      suggestions.push('建议减少同时显示的视频画面数量');
    }
    
    // 音频质量建议
    if (qualityReport.audioQuality === 'poor') {
      suggestions.push('音频质量较差，建议检查麦克风设备');
      suggestions.push('建议关闭音频增强功能');
      suggestions.push('建议使用耳机避免回声');
    }
    
    this.emit('quality-suggestions', suggestions);
  }
}
```

### 🚀 部署和运维方案

#### Docker容器化部署
```dockerfile
# Dockerfile.frontend
FROM node:18-alpine as builder

WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

```dockerfile
# Dockerfile.backend
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PurestAdmin.Api.Host/PurestAdmin.Api.Host.csproj", "PurestAdmin.Api.Host/"]
COPY ["PurestAdmin.Application/PurestAdmin.Application.csproj", "PurestAdmin.Application/"]
COPY ["PurestAdmin.Core/PurestAdmin.Core.csproj", "PurestAdmin.Core/"]
COPY ["PurestAdmin.SqlSugar/PurestAdmin.SqlSugar.csproj", "PurestAdmin.SqlSugar/"]
RUN dotnet restore "PurestAdmin.Api.Host/PurestAdmin.Api.Host.csproj"

COPY . .
WORKDIR "/src/PurestAdmin.Api.Host"
RUN dotnet build "PurestAdmin.Api.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PurestAdmin.Api.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PurestAdmin.Api.Host.dll"]
```

```dockerfile
# Dockerfile.sfu
FROM python:3.11-slim

WORKDIR /app

# 安装系统依赖
RUN apt-get update && apt-get install -y \
    gcc \
    g++ \
    libopus-dev \
    libvpx-dev \
    libavdevice-dev \
    libavformat-dev \
    libavcodec-dev \
    libavutil-dev \
    && rm -rf /var/lib/apt/lists/*

# 安装Python依赖
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# 复制应用代码
COPY . .

# 创建日志目录
RUN mkdir -p /app/logs

EXPOSE 8000

# 健康检查
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8000/health || exit 1

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000", "--log-config", "logging.conf"]
```

#### Docker Compose编排
```yaml
# docker-compose.yml
version: '3.8'

services:
  # PostgreSQL数据库
  postgres:
    image: postgres:15-alpine
    container_name: medical-postgres
    environment:
      POSTGRES_DB: medical_consultation
      POSTGRES_USER: medical_user
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "5432:5432"
    networks:
      - medical-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U medical_user"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Redis缓存
  redis:
    image: redis:7-alpine
    container_name: medical-redis
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"
    networks:
      - medical-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  # .NET后端API
  backend:
    build:
      context: .
      dockerfile: Dockerfile.backend
    container_name: medical-backend
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=medical_consultation;Username=medical_user;Password=${DB_PASSWORD}
      - Redis__Configuration=redis:6379
      - Jwt__SecretKey=${JWT_SECRET_KEY}
      - SFU__ServerUrl=http://sfu:8000
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    ports:
      - "5000:80"
      - "5001:443"
    volumes:
      - ./logs/backend:/app/logs
    networks:
      - medical-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Python SFU服务器
  sfu:
    build:
      context: ./sfu-server
      dockerfile: Dockerfile.sfu
    container_name: medical-sfu
    environment:
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - LOG_LEVEL=INFO
      - MAX_PARTICIPANTS_PER_ROOM=8
      - RECORDING_ENABLED=true
      - RECORDING_STORAGE_PATH=/app/recordings
    volumes:
      - ./recordings:/app/recordings
      - ./logs/sfu:/app/logs
    ports:
      - "8000:8000"
    networks:
      - medical-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Vue3前端
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.frontend
    container_name: medical-frontend
    depends_on:
      - backend
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    networks:
      - medical-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Nginx反向代理
  nginx:
    image: nginx:alpine
    container_name: medical-nginx
    depends_on:
      - frontend
      - backend
      - sfu
    ports:
      - "8080:80"
      - "8443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
      - ./logs/nginx:/var/log/nginx
    networks:
      - medical-network
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3

networks:
  medical-network:
    driver: bridge

volumes:
  postgres_data:
    driver: local
  redis_data:
    driver: local
```

#### 生产环境配置
```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+;http://+
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    volumes:
      - ./certs:/https:ro
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G

  sfu:
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '4'
          memory: 4G
        reservations:
          cpus: '2'
          memory: 2G

  frontend:
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
```

## 📊 性能优化方案

### 🚀 前端性能优化

#### 1. 代码分割和懒加载
```typescript
// router/index.ts
const routes: RouteRecordRaw[] = [
  {
    path: '/remote',
    component: Layout,
    meta: { title: '远程会诊', permissions: ['remote'] },
    children: [
      {
        path: 'consultation',
        name: 'remote_consultation',
        meta: { title: '会诊管理', permissions: ['remote.consultation'] },
        component: () => import('@/views/remote-consultation/consultation/index.vue')
      },
      {
        path: 'rtc-demo',
        name: 'remote_rtc_demo',
        meta: { title: '音视频演示', permissions: ['remote.rtcDemo'] },
        component: () => import('@/views/remote-consultation/rtc-demo/index.vue')
      }
    ]
  }
];
```

#### 2. 组件虚拟滚动
```vue
<!-- VirtualScrollList.vue -->
<template>
  <div class="virtual-scroll-list" ref="containerRef" @scroll="handleScroll">
    <div class="virtual-spacer" :style="{ height: totalHeight + 'px' }"></div>
    <div class="virtual-content" :style="{ transform: `translateY(${offsetY}px)` }">
      <div 
        v-for="item in visibleItems" 
        :key="item.id"
        class="virtual-item"
        :style="{ height: itemHeight + 'px' }"
      >
        <slot :item="item" :index="item.index"></slot>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';

interface Props {
  items: any[];
  itemHeight: number;
  bufferSize?: number;
}

const props = withDefaults(defineProps<Props>(), {
  bufferSize: 5
});

const containerRef = ref<HTMLElement>();
const scrollTop = ref(0);
const containerHeight = ref(0);

const totalHeight = computed(() => props.items.length * props.itemHeight);

const startIndex = computed(() => 
  Math.floor(scrollTop.value / props.itemHeight)
);

const visibleCount = computed(() => 
  Math.ceil(containerHeight.value / props.itemHeight) + props.bufferSize * 2
);

const visibleItems = computed(() => {
  const start = Math.max(0, startIndex.value - props.bufferSize);
  const end = Math.min(
    props.items.length, 
    startIndex.value + visibleCount.value + props.bufferSize
  );
  
  return props.items.slice(start, end).map((item, index) => ({
    ...item,
    index: start + index
  }));
});

const offsetY = computed(() => 
  Math.max(0, startIndex.value - props.bufferSize) * props.itemHeight
);

function handleScroll(event: Event) {
  const target = event.target as HTMLElement;
  scrollTop.value = target.scrollTop;
}

function updateContainerHeight() {
  if (containerRef.value) {
    containerHeight.value = containerRef.value.clientHeight;
  }
}

onMounted(() => {
  updateContainerHeight();
  window.addEventListener('resize', updateContainerHeight);
});

watch(() => props.items.length, () => {
  // 数据变化时重新计算
  updateContainerHeight();
});
</script>
```

#### 3. 图片懒加载和压缩
```vue
<!-- LazyImage.vue -->
<template>
  <div class="lazy-image-container" ref="containerRef">
    <img
      v-if="loaded"
      :src="src"
      :alt="alt"
      :class="['lazy-image', { 'loaded': loaded }]"
      @load="handleLoad"
      @error="handleError"
    />
    <div v-else class="image-placeholder">
      <el-icon class="loading-icon"><Loading /></el-icon>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';

interface Props {
  src: string;
  alt?: string;
  threshold?: number;
  rootMargin?: string;
}

const props = withDefaults(defineProps<Props>(), {
  threshold: 0.1,
  rootMargin: '50px'
});

const loaded = ref(false);
const containerRef = ref<HTMLElement>();
let observer: IntersectionObserver | null = null;

function handleLoad() {
  loaded.value = true;
}

function handleError() {
  console.error('Image failed to load:', props.src);
}

function createObserver() {
  if (!containerRef.value) return;
  
  observer = new IntersectionObserver(
    (entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          loaded.value = true;
          if (observer) {
            observer.unobserve(entry.target);
          }
        }
      });
    },
    {
      threshold: props.threshold,
      rootMargin: props.rootMargin
    }
  );
  
  observer.observe(containerRef.value);
}

onMounted(() => {
  createObserver();
});

onUnmounted(() => {
  if (observer) {
    observer.disconnect();
  }
});
</script>
```

### ⚡ 后端性能优化

#### 1. 数据库查询优化
```csharp
// Repository/ConsultationRepository.cs
public class ConsultationRepository : IConsultationRepository
{
    private readonly ISqlSugarClient _db;
    
    public ConsultationRepository(ISqlSugarClient db)
    {
        _db = db;
    }
    
    /// <summary>
    /// 优化的会诊列表查询，使用分页和索引
    /// </summary>
    public async Task<PagedResult<ConsultationDto>> GetPagedConsultationsAsync(
        ConsultationQuery query)
    {
        var queryable = _db.Queryable<PUREST_RC_CONSULTATION>()
            .WhereIF(query.ConsultationStatus.HasValue, 
                c => c.CONSULTATION_STATUS == query.ConsultationStatus.Value)
            .WhereIF(query.ApplyDoctorId.HasValue, 
                c => c.APPLY_DOCTOR_ID == query.ApplyDoctorId.Value)
            .WhereIF(query.TargetOrgId.HasValue, 
                c => c.TARGET_ORG_ID == query.TargetOrgId.Value)
            .WhereIF(query.StartTime.HasValue, 
                c => c.CREATED_TIME >= query.StartTime.Value)
            .WhereIF(query.EndTime.HasValue, 
                c => c.CREATED_TIME <= query.EndTime.Value)
            .Where(c => c.IS_DELETED == false);
        
        // 使用覆盖索引，避免回表
        var total = await queryable.CountAsync();
        
        var consultations = await queryable
            .OrderBy(c => c.CREATED_TIME, OrderByType.Desc)
            .Select(c => new ConsultationDto
            {
                Id = c.ID,
                CaseId = c.CASE_ID,
                ConsultationStatus = c.CONSULTATION_STATUS,
                EmergencyLevel = c.EMERGENCY_LEVEL,
                Purpose = c.PURPOSE,
                DesiredStartTime = c.DESIRED_START_TIME,
                DesiredEndTime = c.DESIRED_END_TIME,
                MeetingRoomNo = c.MEETING_ROOM_NO,
                ApplyDoctorName = c.APPLY_DOCTOR_ID.ToString(), // 这里应该关联用户表
                CreatedTime = c.CREATED_TIME
            })
            .ToPageListAsync(query.PageIndex, query.PageSize);
        
        return new PagedResult<ConsultationDto>
        {
            Total = total,
            Items = consultations,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }
    
    /// <summary>
    /// 使用异步流处理大量数据
    /// </summary>
    public async IAsyncEnumerable<ConsultationDto> GetConsultationsStreamAsync(
        ConsultationQuery query)
    {
        var queryable = _db.Queryable<PUREST_RC_CONSULTATION>()
            .WhereIF(query.ConsultationStatus.HasValue, 
                c => c.CONSULTATION_STATUS == query.ConsultationStatus.Value)
            .Where(c => c.IS_DELETED == false)
            .OrderBy(c => c.CREATED_TIME, OrderByType.Desc);
        
        using var dataReader = await queryable.ToDataReaderAsync();
        
        while (await dataReader.ReadAsync())
        {
            yield return new ConsultationDto
            {
                Id = dataReader.GetInt64("ID"),
                CaseId = dataReader.GetInt64("CASE_ID"),
                ConsultationStatus = dataReader.GetString("CONSULTATION_STATUS"),
                EmergencyLevel = dataReader.GetString("EMERGENCY_LEVEL"),
                Purpose = dataReader.GetString("PURPOSE"),
                CreatedTime = dataReader.GetDateTime("CREATED_TIME")
            };
        }
    }
}
```

#### 2. 缓存策略
```csharp
// Services/ConsultationService.cs
public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ConsultationService> _logger;
    
    public ConsultationService(
        IConsultationRepository repository,
        IDistributedCache cache,
        ILogger<ConsultationService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取会诊详情，使用多级缓存
    /// </summary>
    public async Task<ConsultationDetailDto> GetConsultationDetailAsync(long id)
    {
        // 1. 尝试从内存缓存获取
        var cacheKey = $"consultation:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cached))
        {
            try
            {
                return JsonSerializer.Deserialize<ConsultationDetailDto>(cached);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached consultation data");
            }
        }
        
        // 2. 从数据库获取
        var consultation = await _repository.GetByIdAsync(id);
        if (consultation == null)
        {
            throw new EntityNotFoundException(typeof(Consultation), id);
        }
        
        var detail = await BuildConsultationDetailAsync(consultation);
        
        // 3. 缓存结果，设置滑动过期时间
        var cacheOptions = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        
        await _cache.SetStringAsync(cacheKey, 
            JsonSerializer.Serialize(detail), cacheOptions);
        
        return detail;
    }
    
    /// <summary>
    /// 使用缓存预热策略
    /// </summary>
    public async Task PreloadActiveConsultationsAsync()
    {
        var activeConsultations = await _repository
            .GetActiveConsultationsAsync();
        
        var tasks = activeConsultations.Select(async consultation =>
        {
            var cacheKey = $"consultation:{consultation.Id}";
            var detail = await BuildConsultationDetailAsync(consultation);
            
            var cacheOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
            };
            
            await _cache.SetStringAsync(cacheKey, 
                JsonSerializer.Serialize(detail), cacheOptions);
        });
        
        await Task.WhenAll(tasks);
        
        _logger.LogInformation("Preloaded {Count} active consultations to cache", 
            activeConsultations.Count);
    }
}
```

### 📈 监控和告警

#### 1. 应用性能监控
```typescript
// services/monitoring.ts
export class ApplicationMonitor {
  private static instance: ApplicationMonitor;
  private metrics: Map<string, number> = new Map();
  private performanceObserver: PerformanceObserver | null = null;
  
  static getInstance(): ApplicationMonitor {
    if (!ApplicationMonitor.instance) {
      ApplicationMonitor.instance = new ApplicationMonitor();
    }
    return ApplicationMonitor.instance;
  }
  
  init(): void {
    this.initPerformanceObserver();
    this.initErrorTracking();
    this.initUserTracking();
  }
  
  private initPerformanceObserver(): void {
    if ('PerformanceObserver' in window) {
      this.performanceObserver = new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
          this.trackPerformanceEntry(entry);
        }
      });
      
      this.performanceObserver.observe({
        entryTypes: ['navigation', 'resource', 'paint', 'measure']
      });
    }
  }
  
  private trackPerformanceEntry(entry: PerformanceEntry): void {
    switch (entry.entryType) {
      case 'navigation':
        this.trackNavigationTiming(entry as PerformanceNavigationTiming);
        break;
      case 'resource':
        this.trackResourceTiming(entry as PerformanceResourceTiming);
        break;
      case 'paint':
        this.trackPaintTiming(entry as PerformancePaintTiming);
        break;
    }
  }
  
  private trackNavigationTiming(timing: PerformanceNavigationTiming): void {
    const metrics = {
      'navigation/domContentLoaded': timing.domContentLoadedEventEnd - timing.domContentLoadedEventStart,
      'navigation/loadComplete': timing.loadEventEnd - timing.loadEventStart,
      'navigation/ttfb': timing.responseStart - timing.requestStart,
      'navigation/download': timing.responseEnd - timing.responseStart
    };
    
    Object.entries(metrics).forEach(([key, value]) => {
      if (value > 0) {
        this.recordMetric(key, value);
        this.sendMetric(key, value);
      }
    });
  }
  
  private trackResourceTiming(timing: PerformanceResourceTiming): void {
    if (timing.duration > 1000) { // 超过1秒的请求
      this.recordMetric(`resource/${timing.name}/duration`, timing.duration);
      this.sendMetric(`resource/${timing.name}/duration`, timing.duration);
    }
  }
  
  private trackPaintTiming(timing: PerformancePaintTiming): void {
    this.recordMetric(`paint/${timing.name}`, timing.startTime);
    this.sendMetric(`paint/${timing.name}`, timing.startTime);
  }
  
  private initErrorTracking(): void {
    window.addEventListener('error', (event) => {
      this.trackError(event.error, event.message, event.filename, event.lineno);
    });
    
    window.addEventListener('unhandledrejection', (event) => {
      this.trackError(event.reason, 'Unhandled Promise Rejection');
    });
  }
  
  private trackError(error: any, message: string, filename?: string, lineno?: number): void {
    const errorInfo = {
      type: error?.name || 'Error',
      message: error?.message || message,
      stack: error?.stack,
      filename,
      lineno,
      timestamp: Date.now(),
      url: window.location.href,
      userAgent: navigator.userAgent
    };
    
    this.sendError(errorInfo);
  }
  
  private recordMetric(key: string, value: number): void {
    this.metrics.set(key, value);
  }
  
  private sendMetric(key: string, value: number): void {
    // 发送到监控服务器
    if (navigator.sendBeacon) {
      const data = JSON.stringify({
        type: 'metric',
        key,
        value,
        timestamp: Date.now(),
        url: window.location.href
      });
      
      navigator.sendBeacon('/api/monitoring/metrics', data);
    }
  }
  
  private sendError(errorInfo: any): void {
    // 发送到错误收集服务器
    if (navigator.sendBeacon) {
      const data = JSON.stringify({
        type: 'error',
        error: errorInfo,
        timestamp: Date.now(),
        url: window.location.href
      });
      
      navigator.sendBeacon('/api/monitoring/errors', data);
    }
  }
  
  private initUserTracking(): void {
    // 跟踪用户行为
    document.addEventListener('click', (event) => {
      const target = event.target as HTMLElement;
      if (target) {
        this.trackUserAction('click', {
          tag: target.tagName,
          id: target.id,
          class: target.className,
          text: target.textContent?.substring(0, 100)
        });
      }
    });
  }
  
  private trackUserAction(action: string, data: any): void {
    this.sendMetric(`user/${action}`, 1);
  }
}
```

#### 2. 后端健康检查
```csharp
// Controllers/HealthController.cs
[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;
    
    public HealthController(
        IHealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var healthResult = await _healthCheckService.CheckHealthAsync();
            
            if (healthResult.Status == HealthStatus.Healthy)
            {
                return Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    checks = healthResult.Entries
                });
            }
            
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                checks = healthResult.Entries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new
            {
                status = "error",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
    
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        // 检查应用是否准备好接收流量
        var checks = new Dictionary<string, object>();
        
        try
        {
            // 数据库连接检查
            var dbHealthy = await CheckDatabaseConnectionAsync();
            checks["database"] = dbHealthy ? "ready" : "not ready";
            
            // Redis连接检查
            var redisHealthy = await CheckRedisConnectionAsync();
            checks["redis"] = redisHealthy ? "ready" : "not ready";
            
            // SFU服务检查
            var sfuHealthy = await CheckSFUConnectionAsync();
            checks["sfu"] = sfuHealthy ? "ready" : "not ready";
            
            var allReady = dbHealthy && redisHealthy && sfuHealthy;
            
            return Ok(new
            {
                status = allReady ? "ready" : "not ready",
                timestamp = DateTime.UtcNow,
                checks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new
            {
                status = "not ready",
                timestamp = DateTime.UtcNow,
                error = ex.Message,
                checks
            });
        }
    }
    
    private async Task<bool> CheckDatabaseConnectionAsync()
    {
        try
        {
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            return connection.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> CheckRedisConnectionAsync()
    {
        try
        {
            var redis = ConnectionMultiplexer.Connect(_configuration["Redis:Configuration"]);
            return redis.IsConnected;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> CheckSFUConnectionAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{_configuration["SFU:ServerUrl"]}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

## 📋 测试策略

### 🧪 单元测试
```typescript
// __tests__/services/medicalRtcService.test.ts
describe('MedicalRtcService', () => {
  let service: MedicalRtcService;
  let mockPeerConnection: RTCPeerConnection;
  let mockLocalStream: MediaStream;
  
  beforeEach(() => {
    // 模拟WebRTC API
    mockPeerConnection = {
      addTrack: jest.fn(),
      close: jest.fn(),
      createOffer: jest.fn().mockResolvedValue({
        sdp: 'mock-sdp',
        type: 'offer'
      }),
      setLocalDescription: jest.fn().mockResolvedValue(undefined),
      getSenders: jest.fn().mockReturnValue([])
    } as any;
    
    mockLocalStream = {
      getTracks: jest.fn().mockReturnValue([
        { kind: 'video', enabled: true },
        { kind: 'audio', enabled: true }
      ])
    } as any;
    
    global.navigator = {
      mediaDevices: {
        getUserMedia: jest.fn().mockResolvedValue(mockLocalStream),
        getDisplayMedia: jest.fn().mockResolvedValue(mockLocalStream)
      }
    } as any;
    
    global.RTCPeerConnection = jest.fn().mockReturnValue(mockPeerConnection) as any;
    
    service = new MedicalRtcService({
      sfuServerUrl: 'ws://localhost:8000',
      consultationId: 'test-123'
    });
  });
  
  afterEach(() => {
    jest.clearAllMocks();
  });
  
  describe('connect', () => {
    it('should successfully connect to medical SFU server', async () => {
      // Arrange
      const mockWebSocket = {
        send: jest.fn(),
        close: jest.fn()
      };
      global.WebSocket = jest.fn().mockImplementation(() => mockWebSocket) as any;
      
      // Act
      await service.connect();
      
      // Assert
      expect(navigator.mediaDevices.getUserMedia).toHaveBeenCalledWith({
        video: {
          width: 1920,
          height: 1080,
          frameRate: 30
        },
        audio: {
          sampleRate: 48000,
          channelCount: 2,
          echoCancellation: true,
          noiseSuppression: true
        }
      });
      
      expect(mockPeerConnection.addTrack).toHaveBeenCalledTimes(2);
      expect(service.getLocalStream()).toBe(mockLocalStream);
    });
    
    it('should handle connection errors gracefully', async () => {
      // Arrange
      navigator.mediaDevices.getUserMedia = jest.fn()
        .mockRejectedValue(new Error('Permission denied'));
      
      // Act & Assert
      await expect(service.connect()).rejects.toThrow('医疗RTC连接失败: Permission denied');
    });
  });
  
  describe('toggleAudio', () => {
    it('should toggle audio track state', async () => {
      // Arrange
      await service.connect();
      const audioTrack = mockLocalStream.getTracks()[1];
      
      // Act
      await service.toggleAudio();
      
      // Assert
      expect(audioTrack.enabled).toBe(false);
      
      // Act - toggle back
      await service.toggleAudio();
      
      // Assert
      expect(audioTrack.enabled).toBe(true);
    });
  });
  
  describe('shareScreen', () => {
    it('should start screen sharing successfully', async () => {
      // Arrange
      await service.connect();
      const mockSender = {
        replaceTrack: jest.fn().mockResolvedValue(undefined)
      };
      mockPeerConnection.getSenders.mockReturnValue([mockSender]);
      
      // Act
      await service.shareScreen();
      
      // Assert
      expect(navigator.mediaDevices.getDisplayMedia).toHaveBeenCalledWith({
        video: {
          width: 1920,
          height: 1080,
          frameRate: 30
        },
        audio: true
      });
      
      expect(mockSender.replaceTrack).toHaveBeenCalled();
    });
    
    it('should handle screen sharing errors', async () => {
      // Arrange
      await service.connect();
      navigator.mediaDevices.getDisplayMedia = jest.fn()
        .mockRejectedValue(new Error('User cancelled'));
      
      // Act & Assert
      await expect(service.shareScreen()).rejects.toThrow('屏幕共享失败: User cancelled');
    });
  });
});
```

### 🔧 集成测试
```csharp
// Tests/ConsultationServiceIntegrationTests.cs
public class ConsultationServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public ConsultationServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateConsultation_Should_Return_Success()
    {
        // Arrange
        var request = new CreateConsultationRequest
        {
            CaseId = 123,
            Purpose = "疑难病例讨论",
            EmergencyLevel = "HIGH",
            DesiredStartTime = DateTime.UtcNow.AddDays(1),
            DesiredEndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            TargetOrgId = 456,
            TargetDepartment = "妇产科",
            TargetExpertId = 789
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/consultation", request);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ConsultationDto>();
        
        Assert.NotNull(result);
        Assert.Equal("疑难病例讨论", result.Purpose);
        Assert.Equal("HIGH", result.EmergencyLevel);
        Assert.Equal("PENDING", result.ConsultationStatus);
    }
    
    [Fact]
    public async Task GetConsultationList_Should_Return_Paginated_Results()
    {
        // Arrange
        var query = new ConsultationQuery
        {
            PageIndex = 1,
            PageSize = 10,
            ConsultationStatus = ConsultationStatus.Pending
        };
        
        // Act
        var response = await _client.GetAsync($"/api/consultation?pageIndex={query.PageIndex}&pageSize={query.PageSize}&status={query.ConsultationStatus}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ConsultationDto>>();
        
        Assert.NotNull(result);
        Assert.True(result.Items.Count <= query.PageSize);
        Assert.All(result.Items, item => Assert.Equal("PENDING", item.ConsultationStatus));
    }
    
    [Fact]
    public async Task JoinConsultation_Should_Return_RTC_Credentials()
    {
        // Arrange
        var consultationId = 1;
        
        // Act
        var response = await _client.PostAsync($"/api/consultation/{consultationId}/join", null);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ConferenceCredentialsDto>();
        
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.RoomId);
        Assert.NotNull(result.SfuServerUrl);
    }
}
```

### 🎯 端到端测试
```typescript
// e2e/consultation-workflow.spec.ts
test.describe('远程会诊完整流程', () => {
  test.beforeEach(async ({ page }) => {
    // 登录系统
    await page.goto('/login');
    await page.fill('#username', 'doctor1');
    await page.fill('#password', 'password123');
    await page.click('#login-button');
    
    // 等待登录成功
    await page.waitForURL('/dashboard');
  });
  
  test('完整的会诊申请和审核流程', async ({ page }) => {
    // 1. 创建会诊申请
    await page.click('text=远程会诊');
    await page.click('text=新建会诊');
    
    // 填写会诊信息
    await page.fill('#patientName', '张小明');
    await page.fill('#medicalRecordNo', 'MR2024001');
    await page.fill('#purpose', '妊娠期糖尿病管理咨询');
    await page.selectOption('#emergencyLevel', 'HIGH');
    await page.fill('#desiredStartTime', '2024-12-20T14:00');
    await page.selectOption('#targetDepartment', '妇产科');
    
    // 上传附件
    await page.setInputFiles('#attachments', [
      'test-files/medical-record.pdf',
      'test-files/lab-report.pdf'
    ]);
    
    // 提交申请
    await page.click('#submit-application');
    
    // 等待提交成功
    await page.waitForSelector('text=会诊申请已提交');
    
    // 2. 审核会诊申请
    await page.click('text=退出'); // 退出当前用户
    
    // 使用审核员账号登录
    await page.goto('/login');
    await page.fill('#username', 'auditor1');
    await page.fill('#password', 'password123');
    await page.click('#login-button');
    
    // 进入审核页面
    await page.click('text=会诊审核');
    await page.click('text=待审核');
    
    // 找到刚才的申请
    await page.click('text=妊娠期糖尿病管理咨询');
    
    // 审核通过
    await page.click('#approve-button');
    await page.fill('#auditComment', '同意申请，请安排专家会诊');
    await page.click('#confirm-audit');
    
    // 等待审核成功
    await page.waitForSelector('text=审核完成');
    
    // 3. 专家加入会诊
    await page.click('text=退出');
    
    // 使用专家账号登录
    await page.goto('/login');
    await page.fill('#username', 'expert1');
    await page.fill('#password', 'password123');
    await page.click('#login-button');
    
    // 进入会诊列表
    await page.click('text=我的会诊');
    await page.click('text=妊娠期糖尿病管理咨询');
    
    // 加入音视频会诊
    await page.click('#join-consultation');
    
    // 等待音视频界面加载
    await page.waitForSelector('#video-container');
    
    // 验证音视频功能
    await expect(page.locator('#local-video')).toBeVisible();
    await expect(page.locator('#remote-video')).toBeVisible();
    
    // 测试屏幕共享
    await page.click('#share-screen');
    await page.waitForSelector('text=屏幕共享已开启');
    
    // 结束会诊
    await page.click('#end-consultation');
    await page.click('#confirm-end');
    
    // 等待结束成功
    await page.waitForSelector('text=会诊已结束');
  });
  
  test('会诊报告生成和签署流程', async ({ page }) => {
    // 1. 进入已结束的会诊
    await page.click('text=已结束会诊');
    await page.click('text=妊娠期糖尿病管理咨询');
    
    // 2. 生成会诊报告
    await page.click('#generate-report');
    
    // 填写报告内容
    await page.fill('#summary', '患者妊娠期糖尿病诊断明确');
    await page.fill('#diagnosis', '妊娠期糖尿病');
    await page.fill('#treatmentSuggestion', '建议饮食控制，监测血糖');
    await page.fill('#medicationSuggestion', '必要时使用胰岛素治疗');
    await page.fill('#followUpSuggestion', '每周复查血糖，定期产检');
    
    // 添加专家意见
    await page.click('#add-expert-opinion');
    await page.fill('#expert-opinion-1', '同意诊断，建议加强血糖监测');
    
    // 保存报告
    await page.click('#save-report');
    await page.waitForSelector('text=报告已保存');
    
    // 3. 专家签署报告
    await page.click('#sign-report');
    await page.fill('#signature-password', 'signature123');
    await page.click('#confirm-signature');
    
    // 等待签署成功
    await page.waitForSelector('text=报告已签署');
    
    // 4. 验证报告内容
    await expect(page.locator('#report-status')).toContainText('已签署');
    await expect(page.locator('#report-summary')).toContainText('患者妊娠期糖尿病诊断明确');
    await expect(page.locator('#signature-info')).toContainText('专家1');
  });
});
```

## 📊 项目成功指标

### 🎯 技术指标
```
✅ 系统可用性：99.9%
✅ 音视频延迟：< 200ms
✅ 1080P视频质量：30fps稳定
✅ 48kHz音频质量：无杂音、无回声
✅ 并发用户支持：> 1000
✅ 响应时间：< 500ms
✅ 数据准确性：100%
✅ 安全合规：符合医疗行业标准
```

### 🏥 业务指标
```
✅ 会诊申请处理时间：< 2分钟
✅ 审核响应时间：< 4小时
✅ 音视频连接成功率：> 98%
✅ 报告生成时间：< 30分钟
✅ 用户满意度：> 90%
✅ 系统培训时间：< 4小时
✅ 运维复杂度：低
✅ 扩展性：支持新功能快速迭代
```

## 🔮 未来扩展规划

### 📅 短期扩展（3个月内）
1. **AI辅助诊断集成**
   - 集成医学影像AI分析
   - 智能病历质控
   - 辅助诊断建议

2. **移动端支持**
   - 开发移动APP
   - 支持移动端音视频
   - 离线功能支持

3. **多语言支持**
   - 英文界面支持
   - 少数民族语言支持
   - 国际化适配

### 📅 中期扩展（6个月内）
1. **高级分析功能**
   - 会诊质量分析
   - 专家绩效评估
   - 运营数据洞察

2. **第三方系统集成**
   - EMR系统深度集成
   - 医保系统对接
   - 药品系统联动

3. **区块链存证**
   - 会诊记录上链
   - 电子签名验证
   - 数据不可篡改

### 📅 长期扩展（12个月内）
1. **智能推荐系统**
   - 专家智能匹配
   - 最佳会诊时间推荐
   - 个性化治疗方案

2. **大数据平台**
   - 区域医疗数据分析
   - 疾病趋势预测
   - 公共卫生监测

3. **云原生架构升级**
   - 微服务架构重构
   - 容器化部署优化
   - 自动扩缩容

## 📋 风险评估与应对

### ⚠️ 技术风险
```
风险1: WebRTC兼容性问题
影响: 部分浏览器无法正常进行音视频通话
概率: 中等
应对: 
  - 提供多种浏览器支持方案
  - 实现降级方案（纯音频模式）
  - 提供详细的用户环境检测

风险2: 网络质量不稳定
影响: 音视频卡顿、延迟
概率: 高
应对:
  - 实现智能网络适应算法
  - 提供网络质量检测和提示
  - 支持多种分辨率自动切换
```

### ⚠️ 业务风险
```
风险1: 用户接受度低
影响: 系统推广困难
概率: 中等
应对:
  - 提供充分的用户培训
  - 设计简洁易用的界面
  - 提供完善的用户支持

风险2: 数据安全风险
影响: 患者隐私泄露
概率: 低
应对:
  - 实施严格的数据加密
  - 建立完善的安全审计机制
  - 定期进行安全评估
```

### ⚠️ 合规风险
```
风险1: 医疗法规变化
影响: 系统需要改造以符合新法规
概率: 中等
应对:
  - 密切关注政策变化
  - 设计灵活的合规配置
  - 建立法规变更响应机制

风险2: 数据跨境传输限制
影响: 云服务使用受限
概率: 低
应对:
  - 采用本地化部署方案
  - 实施数据本地化存储
  - 建立数据出境审批流程
```

## 📋 项目交付清单

### 📦 代码交付
```
✅ 前端源代码 (Vue3 + TypeScript)
✅ 后端源代码 (.NET 8 + ABP)
✅ SFU服务器代码 (Python + aiortc)
✅ 数据库脚本和迁移文件
✅ Docker容器化配置文件
✅ CI/CD流水线配置
✅ 单元测试和集成测试代码
✅ API文档和SDK
```

### 📚 文档交付
```
✅ 系统架构设计文档
✅ 数据库设计文档
✅ API接口文档
✅ 部署运维手册
✅ 用户操作手册
✅ 系统管理员手册
✅ 测试报告和验收文档
✅ 安全评估报告
```

### 🛠️ 环境交付
```
✅ 开发环境搭建指南
✅ 测试环境配置
✅ 预生产环境部署
✅ 生产环境配置
✅ 监控告警配置
✅ 备份恢复方案
✅ 灾难恢复计划
```

### 👥 培训交付
```
✅ 最终用户培训
✅ 系统管理员培训
✅ 运维人员培训
✅ 开发人员培训
✅ 培训视频和材料
✅ 常见问题解答
✅ 技术支持体系建立
```

---

## 🎉 总结

本AGENT开发方案基于现有功能实现优先级方案、开源RTC SFU方案和可行性分析，制定了一个完整、可执行、能够完全自主运行的开发计划。方案涵盖了从架构设计到部署运维的全生命周期，确保项目能够按时、高质量地交付。

### 🚀 核心亮点
1. **完整闭环**: 实现从会诊申请到报告归档的完整业务流程
2. **医疗级质量**: 1080P高清视频 + 48kHz高保真音频
3. **完全自主**: 基于开源技术栈，无厂商锁定
4. **可扩展架构**: 支持未来功能扩展和性能提升
5. **安全合规**: 符合医疗行业数据安全和隐私保护要求

### 📞 技术支持
如需技术支持或有任何疑问，请联系项目团队：
- 技术负责人：[技术负责人姓名]
- 项目经理：[项目经理姓名]
- 技术支持邮箱：support@medical-consultation.com
- 紧急联系电话：400-123-4567

---

**文档版本**: v1.0  
**最后更新**: 2024年12月19日  
**文档状态**: 正式发布  
**下次评审**: 2024年12月26日