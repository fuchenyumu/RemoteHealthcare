import { http } from "@/utils/http";
import type { ApiResponse, PagedResult } from "./types";

const modulePrefix = "/rc-consultation-evaluation";

export interface RcEvaluation {
  id: number;
  consultationId: number;
  patientName: string;
  score: number;
  tags: string;
  content: string;
  isAnonymous: boolean;
  createTime: string;
}

export interface RcEvaluationQuery {
  consultationId?: number;
  pageIndex?: number;
  pageSize?: number;
}

export const getEvaluationPage = (params: RcEvaluationQuery) =>
  http.request<ApiResponse<PagedResult<RcEvaluation>>>("get", modulePrefix, {
    params
  });

export const getEvaluation = (id: number) =>
  http.request<ApiResponse<RcEvaluation>>("get", `${modulePrefix}/${id}`);

export const createEvaluation = (data: any) =>
  http.request<ApiResponse<number>>("post", modulePrefix, { data });
