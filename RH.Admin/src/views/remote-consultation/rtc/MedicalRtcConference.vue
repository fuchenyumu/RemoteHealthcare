<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { ElButton, ElCard, ElMessage, ElTag } from "element-plus";

import { getRtcRoom, getRtcRooms, getRtcServiceStatus } from "@/api/rc/rtc";
import type { ApiResponse, RtcRoomStatus, RtcServiceStatus } from "@/api/rc/types";
import { useMedicalRtcSession } from "@/services/medicalRtcService";

const props = defineProps<{
  consultationId: number;
}>();

const unwrap = <T,>(resp: ApiResponse<T> | T): T => {
  if (resp && typeof resp === "object" && "data" in (resp as ApiResponse<T>)) {
    return (resp as ApiResponse<T>).data;
  }
  return resp as T;
};

const {
  connectionState,
  participants,
  localStream,
  remoteStreams,
  currentToken,
  errorMessage,
  mediaPermission,
  hasMediaPermissions,
  join,
  leave,
  toggleAudio,
  toggleVideo,
  shareScreen,
  requestMediaPermissions
} = useMedicalRtcSession();

const localVideoRef = ref<HTMLVideoElement | null>(null);
const remoteVideoRefs = new Map<string, HTMLVideoElement>();
const joining = ref(false);

const serviceStatus = ref<RtcServiceStatus | null>(null);
const roomStatus = ref<RtcRoomStatus | null>(null);
let monitorTimer: number | null = null;

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

const handleJoin = async () => {
  if (!props.consultationId) return;
  try {
    joining.value = true;
    await join(props.consultationId);
    await refreshRoomSnapshot();
  } catch (error: any) {
    ElMessage.error(error?.message ?? "加入失败");
  } finally {
    joining.value = false;
  }
};

const handleLeave = async () => {
  await leave();
  roomStatus.value = null;
};

const bindRemoteVideo = (el: HTMLVideoElement | null, participantId: string) => {
  if (!el) {
    remoteVideoRefs.delete(participantId);
    return;
  }
  remoteVideoRefs.set(participantId, el);
  const stream = remoteStreams.value.find(item => item.participantId === participantId)?.stream ?? null;
  if (stream) {
    el.srcObject = stream;
  }
};

watch(localStream, stream => {
  if (localVideoRef.value) {
    localVideoRef.value.srcObject = stream ?? null;
  }
});

watch(
  () => remoteStreams.value,
  streams => {
    streams.forEach(item => {
      const el = remoteVideoRefs.get(item.participantId);
      if (el) {
        el.srcObject = item.stream;
      }
    });
  },
  { deep: true }
);

watch(
  () => props.consultationId,
  newId => {
    if (connectionState.value === "connected" && newId) {
      handleJoin();
    }
  }
);

watch(connectionState, state => {
  if (state === "connected") {
    startMonitor();
  } else if (state === "idle") {
    stopMonitor();
  }
});

const participantName = (participantId: string) => {
  return participants.value.find(item => item.participantId === participantId)?.displayName ?? participantId;
};

async function refreshServiceStatus() {
  try {
    serviceStatus.value = unwrap(await getRtcServiceStatus());
  } catch {
    serviceStatus.value = null;
  }
}

async function refreshRoomSnapshot() {
  try {
    const token = currentToken.value;
    if (token?.roomId) {
      roomStatus.value = unwrap(await getRtcRoom(token.roomId));
    } else {
      const rooms = unwrap(await getRtcRooms({ consultationId: props.consultationId }));
      roomStatus.value = rooms[0] ?? null;
    }
  } catch {
    roomStatus.value = null;
  }
}

function startMonitor() {
  stopMonitor();
  refreshServiceStatus();
  refreshRoomSnapshot();
  monitorTimer = window.setInterval(() => {
    refreshServiceStatus();
    refreshRoomSnapshot();
  }, 10000);
}

function stopMonitor() {
  if (monitorTimer) {
    window.clearInterval(monitorTimer);
    monitorTimer = null;
  }
}

onMounted(() => {
  refreshServiceStatus();
});

onBeforeUnmount(() => {
  stopMonitor();
  void leave();
});
</script>

<template>
  <div class="rtc-console">
    <section class="rtc-console__videos">
      <el-card shadow="hover">
        <template #header>
          <div class="flex items-center justify-between">
            <div>音视频会诊</div>
            <div class="flex items-center gap-2">
              <span>连接状态：</span>
              <el-tag :type="stateTagType">{{ connectionState }}</el-tag>
              <span v-if="errorMessage" class="text-danger text-xs">{{ errorMessage }}</span>
            </div>
          </div>
        </template>
        <div class="video-grid">
          <div class="video-tile local">
            <video ref="localVideoRef" autoplay playsinline muted></video>
            <div class="video-label">
              <span>本地</span>
            </div>
          </div>
          <div
            v-for="remote in remoteStreams"
            :key="remote.participantId"
            class="video-tile"
          >
            <video
              autoplay
              playsinline
              :ref="el => bindRemoteVideo(el, remote.participantId)"
            ></video>
            <div class="video-label">
              <span>{{ participantName(remote.participantId) }}</span>
            </div>
          </div>
          <div v-if="remoteStreams.length === 0" class="video-empty">
            <span>等待其他成员加入...</span>
          </div>
        </div>
      </el-card>
    </section>

    <section class="rtc-console__sidebar">
      <el-card shadow="never" class="status-card">
        <template #header>
          <div class="flex items-center justify-between">
            <span>房间状态</span>
            <el-tag v-if="roomStatus?.state" size="small">{{ roomStatus.state }}</el-tag>
          </div>
        </template>
        <ul class="status-list">
          <li>房间号：{{ roomStatus?.roomId ?? "未加入" }}</li>
          <li>在线成员：{{ roomStatus?.participantCount ?? 0 }}</li>
          <li>服务在线房间：{{ serviceStatus?.roomCount ?? "-" }}</li>
          <li>服务参与人数：{{ serviceStatus?.participantCount ?? "-" }}</li>
        </ul>
        <div class="participants">
          <h4>成员列表</h4>
          <ul>
            <li v-for="item in participants" :key="item.participantId">
              <el-tag size="small">{{ item.role }}</el-tag>
              <span class="ml-2">{{ item.displayName }}</span>
            </li>
          </ul>
        </div>
      </el-card>
    </section>

    <section class="rtc-console__controls">
      <el-button type="primary" @click="handleJoin" :loading="joining" v-if="connectionState !== 'connected'">
        加入会诊
      </el-button>
      <el-button type="danger" @click="handleLeave" v-else>离开会诊</el-button>
      <el-button
        @click="() => requestMediaPermissions(true)"
        :disabled="hasMediaPermissions"
      >
        请求摄像头/麦克风
      </el-button>
      <el-button @click="toggleAudio" :disabled="connectionState !== 'connected'">
        切换麦克风
      </el-button>
      <el-button @click="toggleVideo" :disabled="connectionState !== 'connected'">
        切换摄像头
      </el-button>
      <el-button @click="shareScreen" :disabled="connectionState !== 'connected'">
        屏幕共享
      </el-button>
      <span class="permission-hint">
        当前权限：{{ mediaPermission }}
      </span>
    </section>
  </div>
</template>

<style scoped>
.rtc-console {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.rtc-console__videos,
.rtc-console__sidebar {
  width: 100%;
}

.video-grid {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
}

.video-tile {
  position: relative;
  background: #0f172a;
  border-radius: 12px;
  min-height: 200px;
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.08);
}

.video-tile video {
  width: 100%;
  height: 100%;
  object-fit: cover;
  background: #000;
}

.video-label {
  position: absolute;
  left: 12px;
  bottom: 12px;
  background: rgba(0, 0, 0, 0.55);
  color: #fff;
  padding: 4px 8px;
  border-radius: 8px;
  font-size: 12px;
}

.video-empty {
  min-height: 200px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: rgba(255, 255, 255, 0.6);
  border: 1px dashed rgba(255, 255, 255, 0.2);
  border-radius: 12px;
}

.rtc-console__sidebar {
  display: flex;
  gap: 16px;
}

.status-card {
  width: 100%;
}

.status-list {
  list-style: none;
  padding: 0;
  margin: 0 0 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  color: var(--el-text-color-secondary);
}

.participants ul {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.rtc-console__controls {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.permission-hint {
  align-self: center;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
</style>


