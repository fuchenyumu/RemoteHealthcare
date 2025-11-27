import { http } from "@/utils/http";
import type {
  PatientPackQuery,
  PatientPack,
  PagedResult,
  ApiResponse,
  PatientPackFile
} from "./types";

const modulePrefix = "/rc-patient-pack";

export const getPatientPackPage = (params: PatientPackQuery) =>
  http.request<ApiResponse<PagedResult<PatientPack>>>(
    "get",
    `${modulePrefix}/paged-list`,
    { params }
  );

export const getPatientPack = (id: number) =>
  http.request<ApiResponse<PatientPack>>("get", `${modulePrefix}/${id}`);

export const createPatientPack = (data: any) =>
  http.request<ApiResponse<number>>("post", modulePrefix, { data });

export const updatePatientPack = (id: number, data: any) =>
  http.request<ApiResponse<void>>("put", `${modulePrefix}/${id}`, { data });

export const deletePatientPack = (id: number) =>
  http.request<ApiResponse<void>>("delete", `${modulePrefix}/${id}`);

export const getPatientPackFiles = (id: number) =>
  http.request<ApiResponse<PatientPackFile[]>>("get", `${modulePrefix}/${id}/files`);

export const uploadPatientPackFile = (id: number, data: FormData) =>
  http.request<ApiResponse<PatientPackFile>>("post", `${modulePrefix}/${id}/files`, {
    data,
    headers: { "Content-Type": "multipart/form-data" }
  });

export const deletePatientPackFile = (packId: number, fileId: number) =>
  http.request<ApiResponse<void>>("delete", `${modulePrefix}/${packId}/files/${fileId}`);

