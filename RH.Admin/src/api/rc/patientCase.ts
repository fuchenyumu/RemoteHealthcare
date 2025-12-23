import { http } from "@/utils/http";
import type {
  PatientCaseQuery,
  PatientCaseSummary,
  PatientCaseDetail,
  PagedResult,
  ApiResponse
} from "./types";

const modulePrefix = "/rc-patient-case";

export const getPatientCasePage = (params: PatientCaseQuery) =>
  http.request<ApiResponse<PagedResult<PatientCaseSummary>>>(
    "get",
    `${modulePrefix}/paged-list`,
    { params }
  );

export const getPatientCaseDetail = (id: number) =>
  http.request<ApiResponse<PatientCaseDetail>>("get", `${modulePrefix}/${id}`);

export const createPatientCase = (data: any) =>
  http.request<ApiResponse<number>>("post", modulePrefix, { data });

export const updatePatientCase = (id: number, data: any) =>
  http.request<ApiResponse<void>>("put", `${modulePrefix}/${id}`, { data });

export const deletePatientCase = (id: number) =>
  http.request<ApiResponse<void>>("delete", `${modulePrefix}/${id}`);
