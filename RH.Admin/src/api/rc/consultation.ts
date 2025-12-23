import { http } from "@/utils/http";
import type {
  ConsultationQuery,
  ConsultationSummary,
  ConsultationDetail,
  PagedResult,
  ApiResponse
} from "./types";

const modulePrefix = "/rc-consultation";

export const getConsultationPage = (params: ConsultationQuery) =>
  http.request<ApiResponse<PagedResult<ConsultationSummary>>>(
    "get",
    `${modulePrefix}/paged-list`,
    { params }
  );

export const getConsultationDetail = (id: number) =>
  http.request<ApiResponse<ConsultationDetail>>("get", `${modulePrefix}/${id}`);

export const createConsultation = (data: any) =>
  http.request<ApiResponse<number>>("post", modulePrefix, { data });

export const updateConsultation = (id: number, data: any) =>
  http.request<ApiResponse<void>>("put", `${modulePrefix}/${id}`, { data });

export const deleteConsultation = (id: number) =>
  http.request<ApiResponse<void>>("delete", `${modulePrefix}/${id}`);

export const auditConsultation = (
  id: number,
  data: {
    approved: boolean;
    comment?: string;
  }
) =>
  http.request<ApiResponse<void>>("post", `${modulePrefix}/${id}/audit`, {
    data
  });

export const scheduleConsultation = (
  id: number,
  data: {
    scheduledStartTime: string;
    scheduledEndTime: string;
    meetingRoomNo?: string;
  }
) =>
  http.request<ApiResponse<void>>("post", `${modulePrefix}/${id}/schedule`, {
    data
  });

export const updateConsultationRtc = (data: {
  consultationId: number;
  meetingRoomNo?: string;
  rtcChannelId?: string;
  rtcVendor?: string;
}) =>
  http.request<ApiResponse<void>>("post", `${modulePrefix}/rtc`, {
    data
  });

export const startConsultation = (id: number, data?: Record<string, unknown>) =>
  http.request<ApiResponse<void>>("post", `${modulePrefix}/${id}/start`, {
    data: data || {}
  });

export const finishConsultation = (id: number, data?: { summary?: string }) =>
  http.request<ApiResponse<void>>("post", `${modulePrefix}/${id}/finish`, {
    data: data || {}
  });

export const closeConsultation = (id: number, data: { closeReason: string }) =>
  http.request<ApiResponse<void>>("post", `${modulePrefix}/${id}/close`, {
    data
  });

// Statistics
export const getConsultationStatsOverview = () =>
  http.request<
    ApiResponse<{
      total: number;
      today: number;
      pendingReview: number;
      finished: number;
    }>
  >("get", `${modulePrefix}/overview`);

export const getConsultationStatsTrend = (days: number = 30) =>
  http.request<ApiResponse<Array<{ date: string; count: number }>>>(
    "get",
    `${modulePrefix}/trend`,
    { params: { days } }
  );

export const getConsultationStatsDistribution = (
  type: "org" | "dept" | "emergency"
) =>
  http.request<ApiResponse<Array<{ name: string; value: number }>>>(
    "get",
    `${modulePrefix}/distribution`,
    { params: { type } }
  );

export const getExpertRanking = (top: number = 5) =>
  http.request<
    ApiResponse<
      Array<{ name: string; count: number; avgResponseHours: number }>
    >
  >("get", `${modulePrefix}/expert-ranking`, { params: { top } });
