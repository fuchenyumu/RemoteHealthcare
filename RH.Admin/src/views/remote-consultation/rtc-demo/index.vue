<script setup lang="ts">
import { ref, onMounted } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { useRoute, useRouter } from "vue-router";
import { getConsultationDetail } from "@/api/rc/consultation";
import type { ConsultationDetail } from "@/api/rc/types";

const route = useRoute();
const router = useRouter();

const loading = ref(false);
const detail = ref<ConsultationDetail | null>(null);
const rtcInfo = ref({ meetingRoomNo: "", rtcChannelId: "", rtcVendor: "" });

const unwrap = <T>(response: any): T => {
  if (response && typeof response === "object" && "data" in response) {
    return (response as { data: T }).data;
  }
  return response as T;
};

const handleMicToggle = () => {
  ElMessage.info("麦克风演示：已切换");
};
const handleCamToggle = () => {
  ElMessage.info("摄像头演示：已切换");
};
const handleEndDemo = async () => {
  await ElMessageBox.confirm("确认结束演示并返回会诊页面？", "提示", { type: "warning" });
  router.push({ path: "/remote/consultation" });
};

onMounted(async () => {
  const id = Number(route.query.consultationId) || 0;
  if (!id) {
    ElMessage.warning("未获取到会诊ID，当前为演示模式");
    return;
  }
  loading.value = true;
  try {
    detail.value = unwrap<ConsultationDetail>(await getConsultationDetail(id));
    rtcInfo.value.meetingRoomNo = detail.value?.meetingRoomNo ?? "";
    rtcInfo.value.rtcChannelId = detail.value?.rtcChannelId ?? "";
    rtcInfo.value.rtcVendor = detail.value?.rtcVendor ?? "";
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="rtc-demo-page h-screen bg-black text-white flex flex-col">
    <div class="px-4 py-2 bg-[#0b1a2b] flex items-center justify-between">
      <div class="flex items-center gap-4">
        <span class="text-lg font-semibold">音视频会诊演示</span>
        <el-tag v-if="rtcInfo.rtcVendor" type="success">{{ rtcInfo.rtcVendor }}</el-tag>
        <el-tag v-if="rtcInfo.meetingRoomNo" type="info">会议室：{{ rtcInfo.meetingRoomNo }}</el-tag>
        <el-tag v-if="rtcInfo.rtcChannelId" type="warning">频道：{{ rtcInfo.rtcChannelId }}</el-tag>
      </div>
      <div class="flex items-center gap-4">
        <el-alert title="当前为演示模式（无真实音视频连接）" type="info" show-icon :closable="false" />
        <el-alert title="合规提示：请勿在演示中展示患者隐私信息" type="warning" show-icon :closable="false" />
      </div>
    </div>

    <div class="flex-1 flex overflow-hidden">
      <div class="flex-1 relative m-4 rounded-lg bg-[#0f0f10] border border-[#1f2a37]">
        <div class="absolute inset-0 flex items-center justify-center">
          <div class="text-center opacity-80">
            <div class="text-2xl mb-2">主讲人演示流</div>
            <div class="text-sm">演示中：{{ detail?.applyDoctorName ?? '—' }}</div>
          </div>
        </div>
      </div>
      <div class="w-[320px] m-4 rounded-lg bg-[#0f0f10] border border-[#1f2a37] p-2 flex flex-col">
        <div class="text-sm text-gray-200 mb-2">专家流列表</div>
        <div class="grid grid-cols-1 gap-2">
          <div class="h-[140px] rounded bg-[#111213] border border-[#1f2a37] flex items-center justify-center">
            <span class="opacity-70">省妇幼专家</span>
          </div>
          <div class="h-[140px] rounded bg-[#111213] border border-[#1f2a37] flex items-center justify-center">
            <span class="opacity-70">市妇幼专家</span>
          </div>
        </div>
      </div>
    </div>

    <div class="px-4 py-3 bg-[#0b1a2b] flex items-center gap-3">
      <el-button type="primary" @click="handleMicToggle">麦克风</el-button>
      <el-button type="primary" @click="handleCamToggle">摄像头</el-button>
      <el-button type="danger" @click="handleEndDemo">结束演示</el-button>
      <el-tag type="info">医疗主题色：#165DFF</el-tag>
      <el-tag type="danger">录制：关闭（仅主持人/管理员可开启）</el-tag>
    </div>
  </div>
  <el-skeleton v-if="loading" :rows="8" animated />
  <div v-else></div>
  
</template>

<style scoped>
.rtc-demo-page :deep(.el-alert) {
  background-color: transparent;
}
</style>