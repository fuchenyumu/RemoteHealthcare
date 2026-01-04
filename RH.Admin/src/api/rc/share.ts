import { http } from "@/utils/http";

const modulePrefix = "/rc-consultation";

export interface CreateShareLinkInput {
  consultationId: number;
  expireHours?: number;
  maxUsageCount?: number;
  visitorDisplayName?: string;
  remark?: string;
}

export interface ShareLink {
  id: number;
  consultationId: number;
  shareToken: string;
  shareUrl: string;
  expireTime: string;
  maxUsageCount: number;
  usedCount: number;
  isEnabled: boolean;
  createTime: string;
}

export interface ValidateShareTokenResponse {
  isValid: boolean;
  consultationId?: number;
  consultation?: {
    id: number;
    patientName: string;
    purpose: string;
    consultationStatus: string;
    scheduledStartTime?: string;
    meetingRoomNo?: string;
  };
  errorMessage?: string;
  visitorDisplayName?: string;
}

export interface ShareTokenRtcInput {
  shareToken: string;
  visitorName?: string;
}

export interface RtcTokenResponse {
  roomId: string;
  token: string;
  expiresIn: number;
  role: string;
  displayName: string;
  userId: string;
  hostId: string;
  signalingUrl: string;
}

/**
 * 创建分享链接
 */
export const createShareLink = (consultationId: number, data: CreateShareLinkInput) => {
  return http.request<ShareLink>("post", `${modulePrefix}/${consultationId}/share`, { data });
};

/**
 * 获取会诊的分享链接列表
 */
export const getShareLinks = (consultationId: number) => {
  return http.request<ShareLink[]>("get", `${modulePrefix}/${consultationId}/share/links`);
};

/**
 * 验证分享链接(无需认证)
 */
export const validateShareToken = (shareToken: string) => {
  return http.request<ValidateShareTokenResponse>("get", `${modulePrefix}/share/validate/${shareToken}`);
};

/**
 * 通过分享Token获取RTC Token(无需认证)
 */
export const getRtcTokenByShareToken = (data: ShareTokenRtcInput) => {
  return http.request<RtcTokenResponse>("post", `${modulePrefix}/share/rtc-token`, { data });
};

/**
 * 禁用/启用分享链接
 */
export const toggleShareLink = (consultationId: number, id: number) => {
  return http.request<boolean>("put", `${modulePrefix}/${consultationId}/share/${id}/toggle`);
};

/**
 * 删除分享链接
 */
export const deleteShareLink = (consultationId: number, id: number) => {
  return http.request<boolean>("delete", `${modulePrefix}/${consultationId}/share/${id}`);
};
