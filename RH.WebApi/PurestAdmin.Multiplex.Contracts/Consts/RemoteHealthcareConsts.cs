namespace PurestAdmin.Multiplex.Contracts.Consts;

/// <summary>
/// 远程会诊领域常量
/// </summary>
public static class RemoteHealthcareConsts
{
    /// <summary>
    /// 字典编码集合
    /// </summary>
    public static class DictCodes
    {
        public const string PatientType = "RC_PATIENT_TYPE";
        public const string PackStatus = "RC_PACK_STATUS";
        public const string ConsultationStatus = "RC_CONSULT_STATUS";
        public const string EmergencyLevel = "RC_EMERGENCY_LEVEL";
        public const string MemberRole = "RC_MEMBER_ROLE";
        public const string MemberStatus = "RC_MEMBER_STATUS";
        public const string AttachmentType = "RC_ATTACHMENT_TYPE";
        public const string SourceSystem = "RC_SOURCE_SYSTEM";
        public const string TimelineEvent = "RC_TIMELINE_EVENT";
        public const string ReportStatus = "RC_REPORT_STATUS";
        public const string SyncTaskType = "RC_SYNC_TYPE";
        public const string SyncTaskStatus = "RC_SYNC_STATUS";
    }

    /// <summary>
    /// 会诊流程状态编码
    /// </summary>
    public static class ConsultationStatus
    {
        public const string Draft = "DRAFT";
        public const string PendingReview = "PENDING_REVIEW";
        public const string Rejected = "REJECTED";
        public const string WaitSchedule = "WAIT_SCHEDULE";
        public const string Scheduled = "SCHEDULED";
        public const string InProgress = "IN_PROGRESS";
        public const string Finished = "FINISHED";
        public const string Closed = "CLOSED";
    }

    /// <summary>
    /// 打包状态
    /// </summary>
    public static class PackStatus
    {
        public const string Pending = "PENDING";
        public const string Processing = "PROCESSING";
        public const string Completed = "COMPLETED";
        public const string Expired = "EXPIRED";
    }

    /// <summary>
    /// 成员状态编码
    /// </summary>
    public static class MemberStatus
    {
        public const string Pending = "PENDING";
        public const string Confirmed = "CONFIRMED";
        public const string InProgress = "IN_PROGRESS";
        public const string Exited = "EXITED";
    }

    /// <summary>
    /// 同步任务状态编码
    /// </summary>
    public static class SyncTaskStatus
    {
        public const string Pending = "PENDING";
        public const string Processing = "PROCESSING";
        public const string Success = "SUCCESS";
        public const string Failed = "FAILED";
    }

    /// <summary>
    /// 会诊时间线事件
    /// </summary>
    public static class TimelineEvents
    {
        public const string Submitted = "SUBMITTED";
        public const string Approved = "APPROVED";
        public const string Rejected = "REJECTED";
        public const string Scheduled = "SCHEDULED";
        public const string Started = "STARTED";
        public const string Ended = "ENDED";
        public const string ReportSigned = "SIGNED";
        public const string Closed = "CLOSED";
    }

    /// <summary>
    /// 报告状态编码
    /// </summary>
    public static class ReportStatus
    {
        public const string Draft = "DRAFT";
        public const string Signing = "SIGNING";
        public const string Signed = "SIGNED";
        public const string Archived = "ARCHIVED";
    }
}

