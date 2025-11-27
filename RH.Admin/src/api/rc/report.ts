import { http } from "@/utils/http";
import type { ConsultationReport, PagedResult, ApiResponse } from "./types";

const modulePrefix = "/rc-consultation-report";

export const getReportPage = (params: { consultationId: number; reportStatus?: string }) =>
  http.request<ApiResponse<PagedResult<ConsultationReport>>>(
    "get",
    modulePrefix,
    { params }
  );

export const getReport = (id: number) =>
  http.request<ApiResponse<ConsultationReport>>("get", `${modulePrefix}/${id}`);

export const saveReport = (data: any) =>
  http.request<ApiResponse<number>>("post", modulePrefix, { data });

export const updateReport = (id: number, data: any) =>
  http.request<ApiResponse<void>>("put", `${modulePrefix}/${id}`, { data });

export const signReport = (data: {
  consultationId: number;
  signerId: number;
  signatureFileId?: number;
  remark?: string;
}) =>
  http.request<ApiResponse<void>>("post", `${modulePrefix}/sign`, { data });

export const deleteReport = (id: number) =>
  http.request<ApiResponse<void>>("delete", `${modulePrefix}/${id}`);
