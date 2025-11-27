import { http } from "@/utils/http";

import type {
  ApiResponse,
  RtcRoomStatus,
  RtcServiceStatus,
  RtcTokenResponse
} from "./types";

const modulePrefix = "/rc-rtc";

export const requestRtcToken = (consultationId: number, data?: { forceRefresh?: boolean }) =>
  http.request<ApiResponse<RtcTokenResponse>>("post", `${modulePrefix}/${consultationId}/token`, {
    data
  });

export const getRtcRooms = (params?: { consultationId?: number }) =>
  http.request<ApiResponse<RtcRoomStatus[]>>("get", `${modulePrefix}/rooms`, {
    params
  });

export const getRtcRoom = (roomId: string) =>
  http.request<ApiResponse<RtcRoomStatus>>("get", `${modulePrefix}/rooms/${roomId}`);

export const getRtcServiceStatus = () =>
  http.request<ApiResponse<RtcServiceStatus>>("get", `${modulePrefix}/status`);


