<script setup lang="ts">
import { nextTick, ref, onMounted, onUnmounted } from "vue";
import { ElRow, ElCol, ElCard, ElStatistic } from "element-plus";
import * as echarts from "echarts";
import {
  getConsultationStatsOverview,
  getConsultationStatsTrend,
  getConsultationStatsDistribution,
  getExpertRanking
} from "@/api/rc/consultation";

const overview = ref({ total: 0, today: 0, pendingReview: 0, finished: 0 });
const trendChartRef = ref<HTMLElement>();
const distChartRef = ref<HTMLElement>();
let trendChart: echarts.ECharts | null = null;
let distChart: echarts.ECharts | null = null;

const experts = ref([]);

const unwrap = <T,>(res: any): T => (res?.data ?? res) as T;

const initCharts = async () => {
  // 1. Trend Chart
  if (trendChartRef.value) {
    trendChart?.dispose();
    trendChart = echarts.init(trendChartRef.value);
    const trendRes = await getConsultationStatsTrend(15);
    const trendData =
      unwrap<Array<{ date: string; count: number }>>(trendRes) || [];
    trendChart.setOption({
      title: { text: "近15日会诊趋势", left: "center" },
      tooltip: { trigger: "axis" },
      xAxis: { type: "category", data: trendData.map(d => d.date) },
      yAxis: { type: "value" },
      series: [
        {
          data: trendData.map(d => d.count),
          type: "line",
          smooth: true,
          areaStyle: { opacity: 0.2 },
          itemStyle: { color: "#409eff" }
        }
      ]
    });
  }

  // 2. Distribution Chart
  if (distChartRef.value) {
    distChart?.dispose();
    distChart = echarts.init(distChartRef.value);
    const distRes = await getConsultationStatsDistribution("emergency");
    const distData =
      unwrap<Array<{ name: string; value: number }>>(distRes) || [];
    distChart.setOption({
      title: { text: "紧急程度分布", left: "center" },
      tooltip: { trigger: "item" },
      legend: { bottom: "0" },
      series: [
        {
          name: "数量",
          type: "pie",
          radius: ["40%", "70%"],
          data: distData,
          emphasis: {
            itemStyle: {
              shadowBlur: 10,
              shadowOffsetX: 0,
              shadowColor: "rgba(0, 0, 0, 0.5)"
            }
          }
        }
      ]
    });
  }
};

const loadData = async () => {
  const overRes = await getConsultationStatsOverview();
  const over = unwrap<{
    total: number;
    today: number;
    pendingReview: number;
    finished: number;
  }>(overRes);
  overview.value = over || overview.value;

  const expertRes = await getExpertRanking(5);
  experts.value =
    unwrap<Array<{ name: string; count: number; avgResponseHours: number }>>(
      expertRes
    ) || [];

  // 确保 DOM 已挂载再初始化图表
  await nextTick();
  await initCharts();
};

const handleResize = () => {
  trendChart?.resize();
  distChart?.resize();
};

onMounted(() => {
  loadData();
  window.addEventListener("resize", handleResize);
});

onUnmounted(() => {
  window.removeEventListener("resize", handleResize);
  trendChart?.dispose();
  distChart?.dispose();
});
</script>

<template>
  <div class="dashboard-container">
    <el-row :gutter="20">
      <el-col :span="6">
        <el-card shadow="never">
          <el-statistic title="总会诊数" :value="overview.total" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never">
          <el-statistic title="今日新增" :value="overview.today" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never">
          <el-statistic title="待审核" :value="overview.pendingReview" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never">
          <el-statistic title="已完成" :value="overview.finished" />
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="20" class="mt-4">
      <el-col :span="16">
        <el-card shadow="never">
          <div ref="trendChartRef" style="height: 400px" />
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="never">
          <div ref="distChartRef" style="height: 400px" />
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="20" class="mt-4">
      <el-col :span="24">
        <el-card shadow="never" title="专家贡献排行">
          <template #header>
            <div class="flex items-center justify-between">
              <span>专家响应贡献排行</span>
            </div>
          </template>
          <el-table :data="experts" style="width: 100%">
            <el-table-column type="index" label="排名" width="80" />
            <el-table-column prop="name" label="专家姓名" />
            <el-table-column prop="count" label="完成会诊数" />
            <el-table-column prop="avgResponseHours" label="平均响应时长(h)" />
          </el-table>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<style scoped>
.dashboard-container {
  padding: 10px;
}
.mt-4 {
  margin-top: 20px;
}
</style>
