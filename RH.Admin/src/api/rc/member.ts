import { http } from "@/utils/http";
import type {
  ConsultationMemberQuery,
  ConsultationMember,
  PagedResult,
  ApiResponse
} from "./types";

const modulePrefix = "/rc-consultation-member";

export const getMemberPage = (params: ConsultationMemberQuery) =>
  http.request<ApiResponse<PagedResult<ConsultationMember>>>(
    "get",
    modulePrefix,
    { params }
  );

export const getMember = (id: number) =>
  http.request<ApiResponse<ConsultationMember>>("get", `${modulePrefix}/${id}`);

export const createMember = (data: any) =>
  http.request<ApiResponse<number>>("post", modulePrefix, { data });

export const updateMember = (id: number, data: any) =>
  http.request<ApiResponse<void>>("put", `${modulePrefix}/${id}`, { data });

export const deleteMember = (id: number) =>
  http.request<ApiResponse<void>>("delete", `${modulePrefix}/${id}`);
