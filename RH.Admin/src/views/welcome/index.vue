<script setup lang="ts">
import { nextTick, onMounted, onUnmounted, ref } from "vue";
import { useECharts, useDark } from "@pureadmin/utils";
import * as echarts from "echarts";
import { getSystemPlatformInfo, SystemPlatformInfo } from "@/api/auth";
import { getLogCount } from "@/api/system/requestLog";
import { getRcHomeDashboard, type RcHomeDashboard } from "@/api/rc/home";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
const defValue = (): SystemPlatformInfo => {
  return {
    frameworkDescription: "",
    osDescription: "",
    osVersion: "",
    osArchitecture: "",
    machineName: "",
    version: ""
  };
};
const systemPlatformInfo = ref<SystemPlatformInfo>(defValue());
const chartRef = ref();
// 兼容dark主题
const { isDark } = useDark();
const { setOptions } = useECharts(chartRef, {
  theme: isDark.value ? "dark" : "default"
});
const chartOptions = {
  tooltip: {
    trigger: "axis",
    axisPointer: {
      type: "shadow"
    }
  },
  yAxis: {
    type: "value"
  }
};
const showRequestCount = () => {
  getLogCount({}).then((result: any) => {
    const aa = { ...chartOptions, ...result };
    setOptions(aa);
  });
};

const router = useRouter();
const dashboard = ref<RcHomeDashboard | null>(null);
const loading = ref(false);
const trendChartRef = ref<HTMLElement>();
const distChartRef = ref<HTMLElement>();
let trendChart: echarts.ECharts | null = null;
let distChart: echarts.ECharts | null = null;

const initRcCharts = () => {
  if (!dashboard.value) return;

  // 重新渲染时先销毁，避免重复 init
  trendChart?.dispose();
  distChart?.dispose();
  trendChart = null;
  distChart = null;

  const trendData = dashboard.value.trend || [];
  if (trendChartRef.value) {
    trendChart = echarts.init(
      trendChartRef.value,
      isDark.value ? "dark" : undefined
    );
    trendChart.setOption({
      title: { text: "近15日会诊趋势", left: "center" },
      tooltip: { trigger: "axis" },
      xAxis: { type: "category", data: trendData.map(d => d.date) },
      yAxis: { type: "value" },
      grid: { left: 40, right: 20, top: 60, bottom: 40 },
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

  const distData = dashboard.value.emergencyDistribution || [];
  if (distChartRef.value) {
    distChart = echarts.init(
      distChartRef.value,
      isDark.value ? "dark" : undefined
    );
    distChart.setOption({
      title: { text: "紧急程度分布", left: "center" },
      tooltip: { trigger: "item" },
      legend: { bottom: 0 },
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

const loadRcDashboard = async () => {
  try {
    loading.value = true;
    const res = await getRcHomeDashboard(15);
    // 兼容两种返回：1) 纯对象 2) { success, data }
    const body = (res as any)?.data ?? (res as any);
    dashboard.value = body as RcHomeDashboard;

    // 等待 DOM 更新后再初始化图表（避免 ref 还没挂载）
    await nextTick();
    requestAnimationFrame(() => initRcCharts());
  } catch (e) {
    ElMessage.warning("首页数据加载失败，请稍后重试");
  } finally {
    loading.value = false;
  }
};

const handleResize = () => {
  trendChart?.resize();
  distChart?.resize();
};

onMounted(() => {
  getSystemPlatformInfo().then((result: SystemPlatformInfo) => {
    systemPlatformInfo.value = result;
  });
  showRequestCount();
  loadRcDashboard();
  window.addEventListener("resize", handleResize);
});

onUnmounted(() => {
  window.removeEventListener("resize", handleResize);
  trendChart?.dispose();
  distChart?.dispose();
});
</script>

<template>
  <div class="welcome-container">
    <el-tabs>
      <el-tab-pane label="远程会诊工作台">
        <el-skeleton :loading="loading" animated>
          <template #default>
            <el-row :gutter="16">
              <el-col :span="6">
                <el-card
                  shadow="never"
                  class="stat-card"
                  @click="router.push('/remote-consultation/consultation')"
                >
                  <div class="stat-title">总会诊数</div>
                  <div class="stat-value">
                    {{ dashboard?.overview?.totalConsultations ?? 0 }}
                  </div>
                  <div class="stat-sub">进入会诊列表</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card
                  shadow="never"
                  class="stat-card"
                  @click="router.push('/remote-consultation/consultation')"
                >
                  <div class="stat-title">今日新增</div>
                  <div class="stat-value">
                    {{ dashboard?.overview?.todayConsultations ?? 0 }}
                  </div>
                  <div class="stat-sub">关注当天申请</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card
                  shadow="never"
                  class="stat-card"
                  @click="router.push('/remote-consultation/consultation')"
                >
                  <div class="stat-title">待审核</div>
                  <div class="stat-value warn">
                    {{ dashboard?.overview?.pendingReviewConsultations ?? 0 }}
                  </div>
                  <div class="stat-sub">尽快处理审核</div>
                </el-card>
              </el-col>
              <el-col :span="6">
                <el-card
                  shadow="never"
                  class="stat-card"
                  @click="router.push('/remote-consultation/sync-task')"
                >
                  <div class="stat-title">失败同步任务</div>
                  <div class="stat-value danger">
                    {{ dashboard?.overview?.failedSyncTasks ?? 0 }}
                  </div>
                  <div class="stat-sub">查看异常任务</div>
                </el-card>
              </el-col>
            </el-row>

            <el-row :gutter="16" class="mt-4">
              <el-col :span="8">
                <el-card
                  shadow="never"
                  class="stat-card"
                  @click="router.push('/remote-consultation/consultation')"
                >
                  <div class="stat-title">已完成会诊</div>
                  <div class="stat-value">
                    {{ dashboard?.overview?.finishedConsultations ?? 0 }}
                  </div>
                  <div class="stat-sub">查看已完成记录</div>
                </el-card>
              </el-col>
              <el-col :span="8">
                <el-card
                  shadow="never"
                  class="stat-card"
                  @click="router.push('/remote-consultation/consultation')"
                >
                  <div class="stat-title">待处理报告</div>
                  <div class="stat-value warn">
                    {{ dashboard?.overview?.pendingReports ?? 0 }}
                  </div>
                  <div class="stat-sub">草稿/签署中</div>
                </el-card>
              </el-col>
              <el-col :span="8">
                <el-card
                  shadow="never"
                  class="stat-card"
                  @click="router.push('/remote-consultation/dashboard')"
                >
                  <div class="stat-title">数据大盘</div>
                  <div class="stat-value">进入</div>
                  <div class="stat-sub">查看趋势与排行</div>
                </el-card>
              </el-col>
            </el-row>

            <el-row :gutter="16" class="mt-4">
              <el-col :span="16">
                <el-card shadow="never">
                  <div ref="trendChartRef" style="height: 360px" />
                </el-card>
              </el-col>
              <el-col :span="8">
                <el-card shadow="never">
                  <div ref="distChartRef" style="height: 360px" />
                </el-card>
              </el-col>
            </el-row>

            <el-row :gutter="16" class="mt-4">
              <el-col :span="12">
                <el-card shadow="never">
                  <template #header>
                    <div class="flex items-center justify-between">
                      <span>近期已排期会诊（7天内）</span>
                      <el-button
                        text
                        type="primary"
                        @click="router.push('/remote-consultation/calendar')"
                      >
                        打开日历
                      </el-button>
                    </div>
                  </template>
                  <el-table
                    :data="dashboard?.upcomingConsultations || []"
                    style="width: 100%"
                    height="320"
                  >
                    <el-table-column
                      prop="scheduledStartTime"
                      label="开始时间"
                      width="170"
                    />
                    <el-table-column prop="patientName" label="患者" />
                    <el-table-column prop="targetExpertName" label="专家" />
                    <el-table-column
                      prop="consultationStatus"
                      label="状态"
                      width="140"
                    />
                  </el-table>
                </el-card>
              </el-col>

              <el-col :span="12">
                <el-card shadow="never">
                  <template #header>
                    <div class="flex items-center justify-between">
                      <span>最新动态</span>
                      <el-button
                        text
                        type="primary"
                        @click="
                          router.push('/remote-consultation/consultation')
                        "
                      >
                        查看详情
                      </el-button>
                    </div>
                  </template>
                  <el-table
                    :data="dashboard?.recentTimelines || []"
                    style="width: 100%"
                    height="320"
                  >
                    <el-table-column
                      prop="createTime"
                      label="时间"
                      width="170"
                    />
                    <el-table-column
                      prop="eventCode"
                      label="事件"
                      width="120"
                    />
                    <el-table-column
                      prop="eventContent"
                      label="内容"
                      show-overflow-tooltip
                    />
                    <el-table-column
                      prop="createByName"
                      label="操作人"
                      width="120"
                    />
                  </el-table>
                </el-card>
              </el-col>
            </el-row>
          </template>
        </el-skeleton>
      </el-tab-pane>

      <el-tab-pane label="系统概况">
        <el-card :shadow="`never`">
          <template #header> 系统信息 </template>
          <div class="descriptions-text" style="font-weight: 600">
            远程会诊管理平台已接入统一鉴权与审计日志，建议定期关注访问曲线与异常请求，保障业务稳定运行。
          </div>
          <el-descriptions :column="3" border>
            <el-descriptions-item
              label="机器名称"
              label-align="right"
              align="center"
            >
              {{ systemPlatformInfo.machineName }}
            </el-descriptions-item>
            <el-descriptions-item
              label="操作系统"
              label-align="right"
              align="center"
            >
              {{ systemPlatformInfo.osDescription }}
            </el-descriptions-item>
            <el-descriptions-item
              label="操作系统版本"
              label-align="right"
              align="center"
            >
              {{ systemPlatformInfo.osVersion }}
            </el-descriptions-item>
            <el-descriptions-item
              label="平台架构"
              label-align="right"
              align="center"
            >
              {{ systemPlatformInfo.osArchitecture }}
            </el-descriptions-item>
            <el-descriptions-item
              label="程序核心框架版本"
              label-align="right"
              align="center"
            >
              {{ systemPlatformInfo.version }}
            </el-descriptions-item>
            <el-descriptions-item
              label="运行框架"
              label-align="right"
              align="center"
            >
              {{ systemPlatformInfo.frameworkDescription }}
            </el-descriptions-item>
          </el-descriptions>
        </el-card>
        <el-card :shadow="`never`" class="table-card mt-4">
          <template #header>系统访问曲线</template>
          <div ref="chartRef" style="width: 100%; height: 35vh" />
        </el-card>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>
<style scoped>
.welcome-container {
  padding: 10px;
}
.mt-4 {
  margin-top: 16px;
}
.stat-card {
  cursor: pointer;
}
.stat-title {
  font-size: 13px;
  opacity: 0.8;
}
.stat-value {
  margin-top: 8px;
  font-size: 28px;
  font-weight: 700;
  line-height: 1.2;
}
.stat-value.warn {
  color: #e6a23c;
}
.stat-value.danger {
  color: #f56c6c;
}
.stat-sub {
  margin-top: 6px;
  font-size: 12px;
  opacity: 0.7;
}
.descriptions-text {
  background: linear-gradient(
    to right,
    #ff0000,
    #0010f3
  ); /*设置渐变的方向从左到右 颜色从ff0000到ffff00*/
  background-clip: border-box;
  -webkit-background-clip: text; /*将设置的背景颜色限制在文字中*/
  -webkit-text-fill-color: transparent; /*给文字设置成透明*/
}
</style>
