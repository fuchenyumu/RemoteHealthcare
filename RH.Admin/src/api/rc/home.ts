import { http } from "@/utils/http";

export interface RcHomeOverview {
  totalConsultations: number;
  todayConsultations: number;
  pendingReviewConsultations: number;
  finishedConsultations: number;
  pendingReports: number;
  failedSyncTasks: number;
}

export interface RcHomeTrendItem {
  date: string;
  count: number;
}

export interface RcHomeDistributionItem {
  name: string;
  value: number;
}

export interface RcHomeExpertRankingItem {
  name: string;
  count: number;
  avgResponseHours: number;
}

export interface RcHomeUpcomingConsultationItem {
  id: number;
  patientName?: string;
  emergencyLevel?: string;
  consultationStatus?: string;
  scheduledStartTime?: string;
  scheduledEndTime?: string;
  applyOrgName?: string;
  targetOrgName?: string;
  targetExpertName?: string;
}

export interface RcHomeTimelineItem {
  id: number;
  consultationId: number;
  eventCode: string;
  eventContent?: string;
  createTime?: string;
  createByName?: string;
}

export interface RcHomeDashboard {
  overview: RcHomeOverview;
  trend: RcHomeTrendItem[];
  emergencyDistribution: RcHomeDistributionItem[];
  expertRanking: RcHomeExpertRankingItem[];
  upcomingConsultations: RcHomeUpcomingConsultationItem[];
  recentTimelines: RcHomeTimelineItem[];
}

const modulePrefix = "/rc-home";

export const getRcHomeDashboard = (days: number = 15) =>
  // 后端当前返回为“纯对象”，不是 { success, data } 包装，这里按实际返回类型声明
  // 若后续改回 ApiResponse 包装，可在页面侧做 data 兼容读取
  http.request<RcHomeDashboard>(
    "get",
    `${modulePrefix}/dashboard`,
    {
      params: { days }
    }
  );
