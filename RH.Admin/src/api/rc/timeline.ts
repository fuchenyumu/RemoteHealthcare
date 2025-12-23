import { http } from "@/utils/http";
import type {
  ConsultationTimelineQuery,
  ConsultationTimeline,
  PagedResult,
  ApiResponse
} from "./types";

const modulePrefix = "/rc-consultation-timeline";

export const getTimelinePage = (params: ConsultationTimelineQuery) =>
  http.request<ApiResponse<PagedResult<ConsultationTimeline>>>(
    "get",
    `${modulePrefix}/paged-list`,
    { params }
  );

export const getTimeline = (id: number) =>
  http.request<ApiResponse<ConsultationTimeline>>(
    "get",
    `${modulePrefix}/${id}`
  );
