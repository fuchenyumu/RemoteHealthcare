import { computed, onUnmounted, ref, shallowRef } from "vue";

import { requestRtcToken } from "@/api/rc/rtc";
import type {
  ApiResponse,
  RtcRoomParticipant,
  RtcTokenResponse
} from "@/api/rc/types";

type ConnectionState = "idle" | "connecting" | "connected" | "error";
type MediaPermissionState =
  | "unknown"
  | "granted"
  | "audio-only"
  | "denied"
  | "unsupported";

interface MedicalRtcSessionOptions {
  iceServers?: RTCIceServer[];
  mediaConstraints?: MediaStreamConstraints;
}

interface RemoteStreamRef {
  participantId: string;
  stream: MediaStream;
}

const unwrap = <T>(resp: ApiResponse<T> | T): T => {
  if (resp && typeof resp === "object" && "data" in (resp as ApiResponse<T>)) {
    return (resp as ApiResponse<T>).data;
  }
  return resp as T;
};

export function useMedicalRtcSession(options?: MedicalRtcSessionOptions) {
  const connectionState = ref<ConnectionState>("idle");
  const participants = ref<RtcRoomParticipant[]>([]);
  const localStream = shallowRef<MediaStream | null>(null);
  const remoteStreams = ref<RemoteStreamRef[]>([]);
  const currentToken = ref<RtcTokenResponse | null>(null);
  const errorMessage = ref("");
  const mediaPermission = ref<MediaPermissionState>("unknown");
  const reconnecting = ref(false);
  const reconnectAttempts = ref(0);

  const remoteStreamMap = new Map<string, MediaStream>();
  const peerConnections = new Map<string, RTCPeerConnection>();

  let websocket: WebSocket | null = null;
  let pingTimer: number | null = null;
  let reconnectTimer: number | null = null;
  let desiredConsultationId: number | null = null;
  let manualClose = false;
  let allowAutoReconnect = false;

  const MAX_RECONNECT_ATTEMPTS = 5;
  const BASE_RECONNECT_DELAY_MS = 1000;
  const MAX_RECONNECT_DELAY_MS = 15000;

  function clearReconnectTimer() {
    if (reconnectTimer) {
      window.clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }
  }

  function resetPeers() {
    peerConnections.forEach(pc => pc.close());
    peerConnections.clear();
    remoteStreamMap.clear();
    updateRemoteStreams();
    participants.value = [];
  }

  function scheduleReconnect(reason = "连接已断开") {
    if (!allowAutoReconnect || manualClose) return;
    if (!desiredConsultationId) return;
    if (reconnectTimer) return;

    if (reconnectAttempts.value >= MAX_RECONNECT_ATTEMPTS) {
      reconnecting.value = false;
      errorMessage.value = `${reason}，已停止自动重连，请点击“加入会诊”重试`;
      connectionState.value = "error";
      return;
    }

    const attempt = reconnectAttempts.value + 1;
    const delay = Math.min(
      MAX_RECONNECT_DELAY_MS,
      BASE_RECONNECT_DELAY_MS * Math.pow(2, reconnectAttempts.value)
    );

    reconnecting.value = true;
    errorMessage.value = `${reason}，${Math.max(1, Math.round(delay / 1000))}秒后自动重连（${attempt}/${MAX_RECONNECT_ATTEMPTS}）...`;

    reconnectTimer = window.setTimeout(async () => {
      reconnectTimer = null;
      reconnectAttempts.value = attempt;
      try {
        // 连续失败后强制刷新 token
        const forceRefresh = attempt >= 3;
        await doJoin(desiredConsultationId as number, forceRefresh, true);
        reconnecting.value = false;
        if (
          errorMessage.value.includes("自动重连") ||
          errorMessage.value.includes("重连")
        ) {
          errorMessage.value = "";
        }
      } catch {
        scheduleReconnect("重连失败");
      }
    }, delay);
  }

  async function doJoin(
    consultationId: number,
    forceRefresh = false,
    isReconnect = false
  ) {
    if (connectionState.value === "connecting") return;
    if (connectionState.value === "connected" && currentToken.value?.roomId) {
      if (currentToken.value.roomId === `rc-${consultationId}`) {
        return;
      }
      await leave();
    }

    try {
      allowAutoReconnect = true;
      desiredConsultationId = consultationId;
      manualClose = false;
      connectionState.value = "connecting";
      if (!isReconnect) {
        reconnectAttempts.value = 0;
        reconnecting.value = false;
        clearReconnectTimer();
        errorMessage.value = "";
      }
      const tokenResp = unwrap(
        await requestRtcToken(consultationId, { forceRefresh })
      );
      currentToken.value = tokenResp;
      const hasMedia = await ensureLocalStream();
      await openWebSocket(tokenResp);
      connectionState.value = "connected";
      if (!hasMedia) {
        errorMessage.value ||= "未能访问本地摄像头，已仅以数据/音频加入";
      }
    } catch (error: any) {
      errorMessage.value = error?.message ?? "连接失败";
      connectionState.value = "error";
      throw error;
    }
  }

  async function join(consultationId: number, forceRefresh = false) {
    return doJoin(consultationId, forceRefresh, false);
  }

  async function joinWithToken(token: RtcTokenResponse) {
    if (connectionState.value === "connecting") return;
    if (connectionState.value === "connected") {
      await leave();
    }

    try {
      allowAutoReconnect = false; // 分享链接不自动重连
      desiredConsultationId = null;
      manualClose = false;
      connectionState.value = "connecting";
      reconnectAttempts.value = 0;
      reconnecting.value = false;
      clearReconnectTimer();
      errorMessage.value = "";

      currentToken.value = token;
      const hasMedia = await ensureLocalStream();
      await openWebSocket(token);
      connectionState.value = "connected";
      if (!hasMedia) {
        errorMessage.value ||= "未能访问本地摄像头，已仅以数据/音频加入";
      }
    } catch (error: any) {
      errorMessage.value = error?.message ?? "连接失败";
      connectionState.value = "error";
      throw error;
    }
  }

  async function leave() {
    allowAutoReconnect = false;
    desiredConsultationId = null;
    reconnecting.value = false;
    reconnectAttempts.value = 0;
    clearReconnectTimer();

    stopPing();
    manualClose = true;
    websocket?.close();
    websocket = null;

    resetPeers();
    currentToken.value = null;

    if (localStream.value) {
      localStream.value.getTracks().forEach(track => track.stop());
      localStream.value = null;
    }

    connectionState.value = "idle";
    manualClose = false;
  }

  async function toggleAudio() {
    if (!localStream.value) {
      await ensureLocalStream();
    }
    localStream.value?.getAudioTracks().forEach(track => {
      track.enabled = !track.enabled;
    });
  }

  async function toggleVideo() {
    if (!localStream.value) {
      await ensureLocalStream();
    }
    localStream.value?.getVideoTracks().forEach(track => {
      track.enabled = !track.enabled;
    });
  }

  async function shareScreen() {
    const screen = await navigator.mediaDevices.getDisplayMedia({
      video: { width: 1920, height: 1080, frameRate: 30 },
      audio: false
    });
    const screenTrack = screen.getVideoTracks()[0];

    peerConnections.forEach(pc => {
      const sender = pc.getSenders().find(s => s.track?.kind === "video");
      if (sender) {
        sender.replaceTrack(screenTrack);
      }
    });

    screenTrack.onended = () => {
      localStream.value?.getVideoTracks().forEach(track => {
        peerConnections.forEach(pc => {
          const sender = pc.getSenders().find(s => s.track?.kind === "video");
          if (sender) {
            sender.replaceTrack(track);
          }
        });
      });
    };
  }

  function updateRemoteStreams() {
    remoteStreams.value = Array.from(remoteStreamMap.entries()).map(
      ([participantId, stream]) => ({
        participantId,
        stream
      })
    );
  }

  async function ensureLocalStream(): Promise<boolean> {
    if (localStream.value) {
      console.log('[ensureLocalStream] Stream already exists:', localStream.value);
      return true;
    }
    const constraints = options?.mediaConstraints ?? defaultConstraints(true);
    console.log('[ensureLocalStream] Requesting stream with constraints:', constraints);
    try {
      localStream.value =
        await navigator.mediaDevices.getUserMedia(constraints);
      console.log('[ensureLocalStream] Stream obtained:', localStream.value);
      mediaPermission.value = constraints.video ? "granted" : "audio-only";
      return true;
    } catch (error: any) {
      console.warn("Failed to get media stream:", error);
      const fallbackAudio =
        constraints.audio ?? defaultConstraints(false).audio;
      // 摄像头不可用时尝试仅音频加入
      if (constraints.video) {
        try {
          localStream.value = await navigator.mediaDevices.getUserMedia({
            audio: fallbackAudio,
            video: false
          });
          console.log('[ensureLocalStream] Fallback audio-only stream obtained:', localStream.value);
          errorMessage.value = "摄像头不可用，已仅开启麦克风";
          mediaPermission.value = "audio-only";
          return true;
        } catch (audioError: any) {
          console.warn("Fallback audio-only stream failed:", audioError);
        }
      }
      errorMessage.value = parseMediaError(error);
      mediaPermission.value = derivePermissionState(error);
      return false;
    }
  }

  async function requestMediaPermissions(
    videoPreferred = true
  ): Promise<boolean> {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      mediaPermission.value = "unsupported";
      errorMessage.value = "当前浏览器不支持音视频权限";
      return false;
    }
    try {
      const stream = await navigator.mediaDevices.getUserMedia(
        defaultConstraints(videoPreferred)
      );
      stream.getTracks().forEach(track => track.stop());
      mediaPermission.value = videoPreferred ? "granted" : "audio-only";
      errorMessage.value = "";
      return true;
    } catch (error: any) {
      mediaPermission.value = derivePermissionState(error);
      errorMessage.value = parseMediaError(error);
      return false;
    }
  }

  const hasMediaPermissions = computed(
    () =>
      mediaPermission.value === "granted" ||
      mediaPermission.value === "audio-only"
  );

  function defaultConstraints(enableVideo: boolean): MediaStreamConstraints {
    return {
      audio: {
        echoCancellation: true,
        noiseSuppression: true
      },
      video: enableVideo
        ? {
            width: { ideal: 1280 },
            height: { ideal: 720 },
            frameRate: { ideal: 30 },
            facingMode: { ideal: "user" }
          }
        : false
    };
  }

  function derivePermissionState(err: any): MediaPermissionState {
    if (!err || typeof err !== "object") return "denied";
    switch (err.name) {
      case "NotAllowedError":
      case "SecurityError":
        return "denied";
      case "NotFoundError":
      case "OverconstrainedError":
      case "NotReadableError":
        return "unsupported";
      default:
        return "denied";
    }
  }

  function parseMediaError(err: any): string {
    if (!err) return "无法获取音视频设备";
    if (err.message) return err.message;
    if (typeof err === "string") return err;
    return "访问音视频设备失败";
  }

  async function openWebSocket(token: RtcTokenResponse) {
    return new Promise<void>((resolve, reject) => {
      websocket = new WebSocket(token.signalingUrl);
      websocket.onopen = () => {
        websocket?.send(
          JSON.stringify({
            token: token.token,
            device: "web",
            sdk_version: "admin-ui"
          })
        );
        startPing();
        resolve();
      };
      websocket.onerror = event => {
        reject(event);
      };
      websocket.onmessage = handleSocketMessage;
      websocket.onclose = () => {
        stopPing();
        resetPeers();
        websocket = null;
        if (manualClose || !allowAutoReconnect) {
          connectionState.value = "idle";
          return;
        }
        connectionState.value = "idle";
        scheduleReconnect("连接已断开");
      };
    });
  }

  function handleSocketMessage(event: MessageEvent) {
    const payload = JSON.parse(event.data ?? "{}");
    switch (payload.type) {
      case "JOINED":
        participants.value = payload.participants ?? [];
        participants.value
          .filter(item => shouldInitiateOffer(item.participantId))
          .forEach(item => initiateOffer(item.participantId));
        break;
      case "PARTICIPANT_JOINED":
        if (payload.participant) {
          const exists = participants.value.some(
            p => p.participantId === payload.participant.participantId
          );
          if (!exists) {
            participants.value = [...participants.value, payload.participant];
          }
          if (shouldInitiateOffer(payload.participant.participantId)) {
            initiateOffer(payload.participant.participantId);
          }
        }
        break;
      case "PARTICIPANT_LEFT":
        if (payload.participantId) {
          participants.value = participants.value.filter(
            p => p.participantId !== payload.participantId
          );
          closePeerConnection(payload.participantId);
        }
        break;
      case "OFFER":
        if (payload.from && payload.payload) {
          handleRemoteOffer(payload.from, payload.payload);
        }
        break;
      case "ANSWER":
        if (payload.from && payload.payload) {
          handleRemoteAnswer(payload.from, payload.payload);
        }
        break;
      case "CANDIDATE":
        if (payload.from && payload.payload) {
          handleRemoteCandidate(payload.from, payload.payload);
        }
        break;
      default:
        break;
    }
  }

  function shouldInitiateOffer(remoteId: string): boolean {
    const selfId = currentToken.value?.userId ?? "";
    if (!selfId || selfId === remoteId) return false;
    const selfNum = Number(selfId);
    const remoteNum = Number(remoteId);
    if (!Number.isNaN(selfNum) && !Number.isNaN(remoteNum)) {
      return selfNum < remoteNum;
    }
    return selfId.localeCompare(remoteId) < 0;
  }

  async function initiateOffer(targetId: string) {
    const pc = await getOrCreatePeerConnection(targetId);
    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);
    sendSignal("OFFER", targetId, { sdp: offer.sdp, type: offer.type });
  }

  async function handleRemoteOffer(
    fromId: string,
    payload: RTCSessionDescriptionInit
  ) {
    const pc = await getOrCreatePeerConnection(fromId);
    await pc.setRemoteDescription(new RTCSessionDescription(payload));
    const answer = await pc.createAnswer();
    await pc.setLocalDescription(answer);
    sendSignal("ANSWER", fromId, { sdp: answer.sdp, type: answer.type });
  }

  async function handleRemoteAnswer(
    fromId: string,
    payload: RTCSessionDescriptionInit
  ) {
    const pc = peerConnections.get(fromId);
    if (!pc) return;
    await pc.setRemoteDescription(new RTCSessionDescription(payload));
  }

  async function handleRemoteCandidate(
    fromId: string,
    payload: RTCIceCandidateInit
  ) {
    const pc = peerConnections.get(fromId);
    if (!pc) return;
    if (payload.candidate) {
      await pc.addIceCandidate(new RTCIceCandidate(payload));
    }
  }

  async function getOrCreatePeerConnection(participantId: string) {
    if (peerConnections.has(participantId)) {
      return peerConnections.get(participantId) as RTCPeerConnection;
    }

    const pc = new RTCPeerConnection({
      iceServers: options?.iceServers ?? [
        { urls: "stun:stun.l.google.com:19302" }
      ]
    });

    pc.onicecandidate = event => {
      if (event.candidate) {
        sendSignal("CANDIDATE", participantId, {
          candidate: event.candidate.candidate,
          sdpMid: event.candidate.sdpMid,
          sdpMLineIndex: event.candidate.sdpMLineIndex,
          usernameFragment: event.candidate.usernameFragment
        });
      }
    };

    pc.ontrack = event => {
      const [stream] = event.streams;
      if (stream) {
        remoteStreamMap.set(participantId, stream);
        updateRemoteStreams();
      }
    };

    pc.onconnectionstatechange = () => {
      if (
        pc.connectionState === "failed" ||
        pc.connectionState === "closed" ||
        pc.connectionState === "disconnected"
      ) {
        closePeerConnection(participantId);
      }
    };

    if (localStream.value) {
      localStream.value.getTracks().forEach(track => {
        pc.addTrack(track, localStream.value as MediaStream);
      });
    }

    peerConnections.set(participantId, pc);
    return pc;
  }

  function closePeerConnection(participantId: string) {
    const pc = peerConnections.get(participantId);
    if (pc) {
      pc.close();
      peerConnections.delete(participantId);
    }
    if (remoteStreamMap.has(participantId)) {
      remoteStreamMap.delete(participantId);
      updateRemoteStreams();
    }
  }

  function sendSignal(
    type: string,
    targetId: string,
    payload: Record<string, unknown>
  ) {
    if (!websocket || websocket.readyState !== WebSocket.OPEN) return;
    websocket.send(
      JSON.stringify({
        type,
        targetId,
        payload
      })
    );
  }

  function startPing() {
    stopPing();
    pingTimer = window.setInterval(() => {
      if (websocket && websocket.readyState === WebSocket.OPEN) {
        websocket.send(JSON.stringify({ type: "PING" }));
      }
    }, 15000);
  }

  function stopPing() {
    if (pingTimer) {
      window.clearInterval(pingTimer);
      pingTimer = null;
    }
  }

  onUnmounted(() => {
    void leave();
  });

  const hasRemoteStreams = computed(() => remoteStreams.value.length > 0);

  return {
    connectionState,
    participants,
    localStream,
    remoteStreams,
    hasRemoteStreams,
    currentToken,
    errorMessage,
    mediaPermission,
    hasMediaPermissions,
    reconnecting,
    reconnectAttempts,
    join,
    joinWithToken,
    leave,
    toggleAudio,
    toggleVideo,
    shareScreen,
    requestMediaPermissions
  };
}
