<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import {
  ElButton,
  ElCard,
  ElInput,
  ElMessage,
  ElTag,
  ElLoading,
  ElDivider
} from "element-plus";
import {
  Lock,
  User,
  VideoCamera,
  Loading,
  CircleCheck,
  CircleClose,
  Warning,
  Microphone
} from "@element-plus/icons-vue";

import {
  validateShareToken,
  getRtcTokenByShareToken,
  type ValidateShareTokenResponse
} from "@/api/rc/share";
import { useMedicalRtcSession } from "@/services/medicalRtcService";

const route = useRoute();
const shareToken = computed(() => route.params.shareToken as string);

// unwrap函数：提取API响应数据
const unwrap = <T,>(response: any): T => {
  if (response && typeof response === "object" && "data" in response) {
    return (response as { data: T }).data;
  }
  return response as T;
};

const validation = ref<ValidateShareTokenResponse | null>(null);
const visitorName = ref("");
const isLoading = ref(false);
const hasJoined = ref(false);
const permissionRequested = ref(false); // 是否已请求过权限
const permissionState = ref<"pending" | "granted" | "denied" | "unsupported">(
  "pending"
);

const {
  connectionState,
  participants,
  localStream,
  remoteStreams,
  errorMessage,
  hasMediaPermissions,
  reconnecting,
  join,
  joinWithToken,
  leave,
  toggleAudio,
  toggleVideo,
  requestMediaPermissions
} = useMedicalRtcSession();

const localVideoRef = ref<HTMLVideoElement | null>(null);
const remoteVideoRefs = new Map<string, HTMLVideoElement>();

const stateTagType = computed(() => {
  switch (connectionState.value) {
    case "connected":
      return "success";
    case "connecting":
      return "warning";
    case "error":
      return "danger";
    default:
      return "info";
  }
});

const connectionText = computed(() => {
  if (reconnecting.value) return "重连中";
  switch (connectionState.value) {
    case "connected":
      return "已连接";
    case "connecting":
      return "连接中";
    case "error":
      return "连接错误";
    default:
      return "未连接";
  }
});

// 验证分享链接
const validateLink = async () => {
  try {
    isLoading.value = true;
    const response = await validateShareToken(shareToken.value);
    validation.value = unwrap<ValidateShareTokenResponse>(response);

    if (!validation.value.isValid) {
      ElMessage.error(validation.value.errorMessage || "分享链接无效");
      return;
    }

    // 设置默认访客名称
    if (validation.value.visitorDisplayName) {
      visitorName.value = validation.value.visitorDisplayName;
    }

    // 验证成功后，自动请求媒体权限（触发浏览器权限提示）
    if (!permissionRequested.value) {
      await requestPermissions();
    }
  } catch (error) {
    console.error("Validation failed:", error);
    ElMessage.error("验证分享链接失败");
  } finally {
    isLoading.value = false;
  }
};

// 请求媒体权限
const requestPermissions = async () => {
  permissionRequested.value = true;

  // 检查HTTPS要求
  const isLocalhost =
    location.hostname === "localhost" || location.hostname === "127.0.0.1";
  const isHTTPS = location.protocol === "https:";

  if (!isLocalhost && !isHTTPS) {
    permissionState.value = "unsupported";
    ElMessage.error({
      message:
        "浏览器要求使用HTTPS协议才能访问摄像头和麦克风。请使用HTTPS地址或localhost访问。",
      duration: 5000,
      showClose: true
    });
    return;
  }

  // 检查浏览器支持
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    permissionState.value = "unsupported";
    ElMessage.error(
      "您的浏览器不支持音视频功能，请使用Chrome、Firefox、Safari或Edge最新版本"
    );
    return;
  }

  try {
    // 请求权限（这会触发浏览器的权限提示）
    const stream = await navigator.mediaDevices.getUserMedia({
      audio: true,
      video: {
        width: { ideal: 1280 },
        height: { ideal: 720 },
        facingMode: { ideal: "user" }
      }
    });

    // 立即停止流（只是为了获取权限）
    stream.getTracks().forEach(track => track.stop());

    permissionState.value = "granted";
    ElMessage.success("摄像头和麦克风权限已授予");
  } catch (error: any) {
    console.error("Permission denied:", error);
    if (
      error.name === "NotAllowedError" ||
      error.name === "PermissionDeniedError"
    ) {
      permissionState.value = "denied";
      ElMessage.warning({
        message:
          "您拒绝了摄像头和麦克风权限。请在浏览器地址栏点击权限图标，允许访问后刷新页面重试。",
        duration: 6000,
        showClose: true
      });
    } else if (error.name === "NotFoundError") {
      permissionState.value = "unsupported";
      ElMessage.error("未检测到摄像头或麦克风设备");
    } else if (error.name === "NotReadableError") {
      permissionState.value = "unsupported";
      ElMessage.error("无法访问摄像头或麦克风，可能被其他应用占用");
    } else {
      permissionState.value = "denied";
      ElMessage.error({
        message: "无法访问摄像头和麦克风：" + error.message,
        duration: 5000,
        showClose: true
      });
    }
  }
};

// 加入会诊
const handleJoin = async () => {
  if (!validation.value?.isValid || !validation.value.consultationId) {
    ElMessage.error("会诊信息无效");
    return;
  }

  // 验证访客名称
  const name = visitorName.value.trim();
  if (!name) {
    ElMessage.warning("请输入您的姓名");
    return;
  }

  // 检查权限状态
  if (permissionState.value === "denied") {
    ElMessage.error(
      "您拒绝了摄像头和麦克风权限。请在浏览器设置中允许访问后刷新页面。"
    );
    return;
  }

  if (permissionState.value === "unsupported") {
    ElMessage.error("您的设备不支持音视频功能");
    return;
  }

  try {
    isLoading.value = true;
    const loadingInstance = ElLoading.service({
      lock: true,
      text: "正在加入会诊...",
      background: "rgba(0, 0, 0, 0.7)"
    });

    // 请求RTC Token
    const rtcResponse = await getRtcTokenByShareToken({
      shareToken: shareToken.value,
      visitorName: name
    });

    const rtcToken = unwrap<any>(rtcResponse);

    // 使用 RTC Token 直接连接到 WebSocket
    // 分享链接已经从后端获取了 token，直接使用
    await joinWithToken(rtcToken);

    hasJoined.value = true;
    loadingInstance.close();
    ElMessage.success("已加入会诊");
  } catch (error) {
    console.error("Join failed:", error);
    ElMessage.error("加入会诊失败: " + (error as Error).message);
  } finally {
    isLoading.value = false;
  }
};

// 离开会诊
const handleLeave = async () => {
  try {
    await leave();
    hasJoined.value = false;
    ElMessage.success("已离开会诊");
  } catch (error) {
    console.error("Leave failed:", error);
    ElMessage.error("离开会诊失败");
  }
};

// 监听本地视频流
watch(localStream, stream => {
  if (localVideoRef.value) {
    localVideoRef.value.srcObject = stream ?? null;
  }
});

// 监听 localVideoRef 的变化，确保流能正确绑定
watch(localVideoRef, videoEl => {
  if (videoEl && localStream.value) {
    videoEl.srcObject = localStream.value;
  }
});

// 绑定远程视频元素
const bindRemoteVideo = (
  el: HTMLVideoElement | null,
  participantId: string
) => {
  if (!el) {
    remoteVideoRefs.delete(participantId);
    return;
  }
  remoteVideoRefs.set(participantId, el);
  const stream =
    remoteStreams.value.find(item => item.participantId === participantId)
      ?.stream ?? null;
  if (stream) {
    el.srcObject = stream;
  }
};

// 监听远程视频流
watch(
  remoteStreams,
  streams => {
    streams.forEach(({ participantId, stream }) => {
      const videoRef = remoteVideoRefs.get(participantId);
      if (videoRef) {
        videoRef.srcObject = stream;
      }
    });
  },
  { deep: true }
);

onMounted(() => {
  validateLink();
});

onBeforeUnmount(async () => {
  if (hasJoined.value) {
    await leave();
  }
});
</script>

<template>
  <div class="share-consultation-container">
    <!-- 未加入状态 -->
    <div v-if="!hasJoined" class="join-panel">
      <ElCard class="validation-card">
        <template #header>
          <div class="card-header">
            <el-icon><Lock /></el-icon>
            <span>会诊分享链接</span>
          </div>
        </template>

        <div v-if="isLoading" class="loading-state">
          <el-icon class="is-loading"><Loading /></el-icon>
          <p>正在验证分享链接...</p>
        </div>

        <div
          v-else-if="
            validation && validation.isValid && validation.consultation
          "
          class="consultation-info"
        >
          <div class="info-section">
            <h3>会诊信息</h3>
            <div class="info-row">
              <span class="label">患者姓名:</span>
              <span class="value">{{
                validation.consultation.patientName
              }}</span>
            </div>
            <div class="info-row">
              <span class="label">会诊目的:</span>
              <span class="value">{{ validation.consultation.purpose }}</span>
            </div>
            <div class="info-row">
              <span class="label">会诊状态:</span>
              <ElTag
                :type="
                  validation.consultation.consultationStatus === 'IN_PROGRESS'
                    ? 'success'
                    : 'info'
                "
              >
                {{
                  validation.consultation.consultationStatus === "IN_PROGRESS"
                    ? "进行中"
                    : "已排期"
                }}
              </ElTag>
            </div>
            <div
              v-if="validation.consultation.scheduledStartTime"
              class="info-row"
            >
              <span class="label">排期时间:</span>
              <span class="value">{{
                new Date(
                  validation.consultation.scheduledStartTime
                ).toLocaleString("zh-CN")
              }}</span>
            </div>
          </div>

          <el-divider />

          <div class="visitor-section">
            <h3>您的信息</h3>
            <ElInput
              v-model="visitorName"
              placeholder="请输入您的姓名"
              :prefix-icon="User"
              maxlength="50"
              show-word-limit
            />
            <p class="tip">提示: 您将以"外部专家"身份参与会诊</p>

            <!-- 权限状态提示 -->
            <div class="permission-status" :class="permissionState">
              <div v-if="permissionState === 'pending'" class="status-item">
                <el-icon class="is-loading"><Loading /></el-icon>
                <span>正在请求摄像头和麦克风权限...</span>
              </div>
              <div
                v-else-if="permissionState === 'granted'"
                class="status-item success"
              >
                <el-icon><CircleCheck /></el-icon>
                <span>摄像头和麦克风权限已授予</span>
              </div>
              <div
                v-else-if="permissionState === 'denied'"
                class="status-item error"
              >
                <el-icon><CircleClose /></el-icon>
                <span
                  >权限被拒绝。请点击浏览器地址栏的权限图标，允许访问摄像头和麦克风，然后刷新页面。</span
                >
              </div>
              <div
                v-else-if="permissionState === 'unsupported'"
                class="status-item warning"
              >
                <el-icon><Warning /></el-icon>
                <span
                  >您的浏览器或设备不支持音视频功能。请使用Chrome、Firefox、Safari或Edge最新版本。</span
                >
              </div>
            </div>
          </div>

          <div class="action-section">
            <ElButton
              type="primary"
              size="large"
              :icon="VideoCamera"
              :loading="isLoading"
              :disabled="!visitorName.trim()"
              @click="handleJoin"
            >
              加入会诊
            </ElButton>
          </div>
        </div>

        <div v-else class="error-state">
          <el-icon class="error-icon"><Warning /></el-icon>
          <p class="error-message">
            {{ validation?.errorMessage || "分享链接无效或已过期" }}
          </p>
        </div>
      </ElCard>
    </div>

    <!-- 已加入状态 - 会诊视频界面 -->
    <div v-else class="conference-panel">
      <div class="conference-header">
        <div class="status-info">
          <ElTag :type="stateTagType">{{ connectionText }}</ElTag>
          <span class="participant-count"
            >在线人数: {{ participants.length }}</span
          >
        </div>
        <div class="conference-info">
          <span v-if="validation?.consultation"
            >{{ validation.consultation.patientName }} 的会诊</span
          >
        </div>
        <ElButton type="danger" @click="handleLeave">离开会诊</ElButton>
      </div>

      <div class="video-grid">
        <!-- 本地视频 -->
        <div class="video-container local-video">
          <video ref="localVideoRef" autoplay muted playsinline />
          <div class="video-label">我 ({{ visitorName }})</div>
          <div class="video-controls">
            <ElButton
              :type="hasMediaPermissions.audio ? 'primary' : 'default'"
              circle
              @click="toggleAudio"
            >
              <el-icon><Microphone /></el-icon>
            </ElButton>
            <ElButton
              :type="hasMediaPermissions.video ? 'primary' : 'default'"
              circle
              @click="toggleVideo"
            >
              <el-icon><VideoCamera /></el-icon>
            </ElButton>
          </div>
        </div>

        <!-- 远程视频 -->
        <div
          v-for="remote in remoteStreams"
          :key="remote.participantId"
          class="video-container remote-video"
        >
          <video
            :ref="el => bindRemoteVideo(el, remote.participantId)"
            autoplay
            playsinline
          />
          <div class="video-label">
            {{
              participants.find(p => p.participantId === remote.participantId)
                ?.displayName || remote.participantId
            }}
          </div>
        </div>
      </div>

      <!-- 错误提示 -->
      <div v-if="errorMessage" class="error-message">
        <el-icon><Warning /></el-icon>
        {{ errorMessage }}
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.share-consultation-container {
  min-height: 100vh;
  background: #f0f4f8;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.join-panel {
  width: 100%;
  max-width: 600px;
}

.validation-card {
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
  border-radius: 16px;

  .card-header {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 18px;
    font-weight: 600;
  }

  .loading-state,
  .error-state {
    text-align: center;
    padding: 40px 20px;

    .el-icon {
      font-size: 48px;
      margin-bottom: 16px;
    }

    .error-icon {
      color: #f56c6c;
    }

    .error-message {
      font-size: 16px;
      color: #606266;
    }
  }

  .consultation-info {
    .info-section {
      h3 {
        margin-bottom: 16px;
        color: #303133;
      }

      .info-row {
        display: flex;
        justify-content: space-between;
        padding: 8px 0;
        border-bottom: 1px solid #ebeef5;

        &:last-child {
          border-bottom: none;
        }

        .label {
          color: #909399;
          font-weight: 500;
        }

        .value {
          color: #303133;
        }
      }
    }

    .visitor-section {
      h3 {
        margin-bottom: 16px;
        color: #303133;
      }

      .tip {
        margin-top: 8px;
        font-size: 12px;
        color: #909399;
      }

      .permission-status {
        margin-top: 16px;
        padding: 12px;
        border-radius: 8px;
        background: #f5f7fa;

        .status-item {
          display: flex;
          align-items: center;
          gap: 8px;
          font-size: 14px;

          .el-icon {
            font-size: 18px;
            flex-shrink: 0;
          }

          &.success {
            color: #67c23a;
            background: #f0f9ff;
            padding: 8px;
            border-radius: 6px;
          }

          &.error {
            color: #f56c6c;
            background: #fef0f0;
            padding: 8px;
            border-radius: 6px;
          }

          &.warning {
            color: #e6a23c;
            background: #fdf6ec;
            padding: 8px;
            border-radius: 6px;
          }
        }
      }
    }

    .action-section {
      margin-top: 24px;
      text-align: center;

      .el-button {
        width: 100%;
      }
    }
  }
}

.conference-panel {
  width: 100%;
  height: 100vh;
  background: #000;
  display: flex;
  flex-direction: column;
}

.conference-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);

  .status-info {
    display: flex;
    align-items: center;
    gap: 12px;
    color: #fff;

    .participant-count {
      font-size: 14px;
    }
  }

  .conference-info {
    color: #fff;
    font-size: 16px;
    font-weight: 600;
  }
}

.video-grid {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 16px;
  padding: 16px;
  overflow-y: auto;
}

.video-container {
  position: relative;
  background: #1a1a1a;
  border-radius: 12px;
  overflow: hidden;
  aspect-ratio: 16 / 9;

  video {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  &.local-video {
    border: 2px solid #409eff;
  }

  .video-label {
    position: absolute;
    bottom: 60px;
    left: 12px;
    background: rgba(0, 0, 0, 0.6);
    color: #fff;
    padding: 6px 12px;
    border-radius: 6px;
    font-size: 14px;
  }

  .video-controls {
    position: absolute;
    bottom: 12px;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    gap: 12px;

    .el-button {
      width: 44px;
      height: 44px;
    }
  }
}

.error-message {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%);
  background: rgba(245, 108, 108, 0.9);
  color: #fff;
  padding: 12px 24px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 8px;
  z-index: 999;
}
</style>
