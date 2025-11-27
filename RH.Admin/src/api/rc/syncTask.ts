import { http } from "@/utils/http";
import type { SyncTaskQuery, SyncTaskItem, PagedResult, ApiResponse } from "./types";

const modulePrefix = "/rc-sync-task";

export const getSyncTaskPage = (params: SyncTaskQuery) =>
  http.request<ApiResponse<PagedResult<SyncTaskItem>>>(
    "get",
    `${modulePrefix}/paged-list`,
    { params }
  );

export const getSyncTask = (id: number) =>
  http.request<ApiResponse<SyncTaskItem>>("get", `${modulePrefix}/${id}`);

export const createSyncTask = (data: any) =>
  http.request<ApiResponse<number>>("post", modulePrefix, { data });

export const updateSyncTask = (id: number, data: any) =>
  http.request<ApiResponse<void>>("put", `${modulePrefix}/${id}`, { data });

export const deleteSyncTask = (id: number) =>
  http.request<ApiResponse<void>>("delete", `${modulePrefix}/${id}`);

