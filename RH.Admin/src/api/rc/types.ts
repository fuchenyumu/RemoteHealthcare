export interface PagingQuery {
  pageIndex?: number;
  pageSize?: number;
}

export interface ConsultationQuery extends PagingQuery {
  keyword?: string;
  consultationStatus?: string;
  emergencyLevel?: string;
  applyDoctorId?: number;
  targetOrgId?: number;
  createTimeStart?: string;
  createTimeEnd?: string;
  scheduledStartTimeStart?: string;
  scheduledStartTimeEnd?: string;
}

export interface ConsultationSummary {
  id: number;
  caseId: number;
  packId?: number;
  packNo?: string;
  applyOrgId?: number;
  patientName?: string;
  patientType?: string;
  emergencyLevel?: string;
  consultationStatus?: string;
  purpose?: string;
  applyDoctorId?: number;
  applyDoctorName?: string;
  targetOrgId?: number;
  applyOrgName?: string;
  targetOrgName?: string;
  targetDepartment?: string;
  targetExpertId?: number;
  targetExpertName?: string;
  desiredStartTime?: string;
  desiredEndTime?: string;
  scheduledStartTime?: string;
  scheduledEndTime?: string;
  meetingRoomNo?: string;
  rtcChannelId?: string;
  rtcVendor?: string;
  createTime?: string;
}

export interface ConsultationDetail extends ConsultationSummary {
  patientCase?: PatientCaseDetail;
  patientPack?: PatientPack;
  members?: ConsultationMember[];
  attachments?: ConsultationAttachment[];
  timelines?: ConsultationTimeline[];
  report?: ConsultationReport;
}

export interface PatientCaseQuery extends PagingQuery {
  keyword?: string;
  patientType?: string;
  dataSource?: string;
}

export interface PatientCaseSummary {
  id: number;
  patientName: string;
  patientSex?: string;
  patientType?: string;
  phone?: string;
  gestationalWeeks?: number;
  childAgeMonths?: number;
  dataSource?: string;
  createTime?: string;
}

export interface PatientCaseDetail extends PatientCaseSummary {
  remark?: string;
  hisPatientId?: string;
  hisVisitId?: string;
  patientBirth?: string;
  idCard?: string;
  contactAddress?: string;
  medicalSummary?: string;
  allergies?: string;
}

export interface PatientPackQuery extends PagingQuery {
  caseId?: number;
  packStatus?: string;
  keyword?: string;
}

export interface PatientPack {
  id: number;
  caseId: number;
  packNo: string;
  packStatus: string;
  expireTime?: string;
  fileCount: number;
  totalSize: number;
  syncTaskId?: number;
  createTime?: string;
  remark?: string;
}

export interface PatientPackFile {
  id: number;
  packId: number;
  fileId: number;
  fileName: string;
  fileSize: number;
  createTime?: string;
}

export interface ConsultationAttachmentQuery extends PagingQuery {
  consultationId: number;
  attachmentType?: string;
}

export interface ConsultationAttachment {
  id: number;
  consultationId: number;
  fileId?: number;
  fileName?: string;
  externalUri?: string;
  attachmentType: string;
  sourceSystem: string;
  description?: string;
  createTime?: string;
}

export interface ConsultationMemberQuery extends PagingQuery {
  consultationId: number;
  roleCode?: string;
  joinStatus?: string;
}

export interface ConsultationMember {
  id: number;
  consultationId: number;
  userId: number;
  userName?: string;
  orgId: number;
  orgName?: string;
  roleCode: string;
  joinStatus: string;
  joinTime?: string;
  leaveTime?: string;
  remark?: string;
}

export interface ConsultationTimelineQuery extends PagingQuery {
  consultationId: number;
  eventCode?: string;
}

export interface ConsultationTimeline {
  id: number;
  consultationId: number;
  eventCode: string;
  eventContent?: string;
  snapshot?: string;
  createTime?: string;
  createBy?: number;
  createByName?: string;
}

export interface ConsultationReport extends Record<string, any> {
  id: number;
  consultationId: number;
  summary: string;
  diagnosis?: string;
  treatmentAdvice?: string;
  followUpPlan?: string;
  reportStatus: string;
  signerId?: number;
  signerName?: string;
  signedTime?: string;
  signatureFileId?: number;
  remark?: string;
}

export interface SyncTaskQuery extends PagingQuery {
  taskType?: string;
  taskStatus?: string;
  keyword?: string;
}

export interface SyncTaskItem {
  id: number;
  taskNo: string;
  taskType: string;
  taskStatus: string;
  requestPayload?: any;
  responsePayload?: any;
  completedTime?: string;
  retryCount: number;
  remark?: string;
  createTime?: string;
}

export interface RtcTokenResponse {
  token: string;
  expiresIn: number;
  roomId: string;
  role: string;
  displayName: string;
  userId: string;
  hostId: string;
  signalingUrl: string;
}

export interface RtcRoomParticipant {
  participantId: string;
  displayName: string;
  role: string;
  joinedAt: string;
  muted: boolean;
}

export interface RtcRoomStatus {
  roomId: string;
  consultationId: number;
  hostId: string;
  state: string;
  participantCount: number;
  createdAt: string;
  closedAt?: string | null;
  participants: RtcRoomParticipant[];
}

export interface RtcServiceStatus {
  status: string;
  app: string;
  roomCount: number;
  participantCount: number;
  startedAt: string;
  uptimeSeconds: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  error?: { message: string } | null;
}

export interface PagedResult<T> {
  pageIndex: number;
  pageSize: number;
  total: number;
  pageCount: number;
  items: T[];
}
