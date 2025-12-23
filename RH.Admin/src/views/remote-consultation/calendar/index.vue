<script setup lang="ts">
import { ref, onMounted } from "vue";
import { ElCalendar, ElCard, ElTag, ElTooltip } from "element-plus";
import dayjs from "dayjs";
import { getConsultationPage } from "@/api/rc/consultation";
import { useRouter } from "vue-router";

const calendarValue = ref(new Date());
const consultations = ref([]);
const router = useRouter();

const loadMonthData = async (date: Date) => {
  const startOfMonth = dayjs(date).startOf("month").format("YYYY-MM-DD");
  const endOfMonth = dayjs(date).endOf("month").format("YYYY-MM-DD");

  // 后端分页限制 pageSize: 5~200，这里按排期时间范围查询当月数据
  const res = await getConsultationPage({
    pageIndex: 1,
    pageSize: 200,
    scheduledStartTimeStart: startOfMonth,
    scheduledStartTimeEnd: endOfMonth
  });
  const body = (res as any)?.data ?? (res as any);
  consultations.value = body?.items || [];
};

const getDayEvents = (date: string) => {
  return consultations.value.filter(item => {
    if (!item.scheduledStartTime) return false;
    return dayjs(item.scheduledStartTime).format("YYYY-MM-DD") === date;
  });
};

const handleDateChange = (val: Date) => {
  loadMonthData(val);
};

const goToDetail = (id: number) => {
  // 演示点击进入详情
  router.push({
    path: "/remote/consultation",
    query: { id }
  });
};

const getTagType = (status: string) => {
  switch (status) {
    case "SCHEDULED":
      return "warning";
    case "IN_PROGRESS":
      return "success";
    case "FINISHED":
      return "info";
    default:
      return "primary";
  }
};

onMounted(() => {
  loadMonthData(calendarValue.value);
});
</script>

<template>
  <div class="calendar-container">
    <el-card shadow="never">
      <template #header>
        <div class="flex items-center justify-between">
          <span>全院会诊排期中心</span>
          <el-tag type="info">演示版：基于排期时间展示</el-tag>
        </div>
      </template>

      <el-calendar v-model="calendarValue" @change="handleDateChange">
        <template #date-cell="{ data }">
          <div class="date-cell">
            <span class="date-num">{{ data.day.split("-").slice(-1)[0] }}</span>
            <div class="event-list">
              <div
                v-for="item in getDayEvents(data.day)"
                :key="item.id"
                class="event-item"
                @click.stop="goToDetail(item.id)"
              >
                <el-tooltip
                  :content="`${item.patientName} - ${item.purpose}`"
                  placement="top"
                >
                  <el-tag
                    :type="getTagType(item.consultationStatus)"
                    size="small"
                    effect="dark"
                    class="w-full"
                  >
                    {{ dayjs(item.scheduledStartTime).format("HH:mm") }}
                    {{ item.patientName }}
                  </el-tag>
                </el-tooltip>
              </div>
            </div>
          </div>
        </template>
      </el-calendar>
    </el-card>
  </div>
</template>

<style scoped>
.calendar-container {
  padding: 10px;
}

.date-cell {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.date-num {
  font-size: 14px;
  font-weight: bold;
  margin-bottom: 4px;
}

.event-list {
  flex: 1;
  overflow-y: auto;
}

.event-item {
  margin-bottom: 2px;
  cursor: pointer;
}

.w-full {
  width: 100%;
  text-align: left;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

:deep(.el-calendar-table .el-calendar-day) {
  height: 120px;
  padding: 4px;
}
</style>
