import { http } from "@/utils/http";
import type {
  ConsultationAttachmentQuery,
  ConsultationAttachment,
  PagedResult,
  ApiResponse
} from "./types";

const modulePrefix = "/rc-consultation-attachment";

export const getAttachmentPage = (params: ConsultationAttachmentQuery) =>
  http.request<ApiResponse<PagedResult<ConsultationAttachment>>>(
    "get",
    modulePrefix,
    { params }
  );

export const getAttachment = (id: number) =>
  http.request<ApiResponse<ConsultationAttachment>>(
    "get",
    `${modulePrefix}/${id}`
  );

export const createAttachment = (data: any) =>
  http.request<ApiResponse<number>>("post", modulePrefix, { data });

export const updateAttachment = (id: number, data: any) =>
  http.request<ApiResponse<void>>("put", `${modulePrefix}/${id}`, { data });

export const deleteAttachment = (id: number) =>
  http.request<ApiResponse<void>>("delete", `${modulePrefix}/${id}`);
