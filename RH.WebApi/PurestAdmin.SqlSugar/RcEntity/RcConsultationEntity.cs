using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 远程会诊主表
/// </summary>
[SugarTable("PUREST_RC_CONSULTATION")]
public class RcConsultationEntity : BaseEntity
{
    [SugarColumn(ColumnName = "case_id")]
    public long CaseId { get; set; }

    [SugarColumn(ColumnName = "pack_id", IsNullable = true)]
    public long? PackId { get; set; }

    [SugarColumn(ColumnName = "apply_org_id")]
    public long ApplyOrgId { get; set; }

    [SugarColumn(ColumnName = "apply_doctor_id")]
    public long ApplyDoctorId { get; set; }

    [SugarColumn(ColumnName = "target_org_id", IsNullable = true)]
    public long? TargetOrgId { get; set; }

    [SugarColumn(ColumnName = "target_department", IsNullable = true)]
    public string TargetDepartment { get; set; }

    [SugarColumn(ColumnName = "target_expert_id", IsNullable = true)]
    public long? TargetExpertId { get; set; }

    [SugarColumn(ColumnName = "purpose", IsNullable = true)]
    public string Purpose { get; set; }

    [SugarColumn(ColumnName = "emergency_level")]
    public string EmergencyLevel { get; set; }

    [SugarColumn(ColumnName = "consultation_status")]
    public string ConsultationStatus { get; set; }

    [SugarColumn(ColumnName = "desired_start_time", IsNullable = true)]
    public DateTime? DesiredStartTime { get; set; }

    [SugarColumn(ColumnName = "desired_end_time", IsNullable = true)]
    public DateTime? DesiredEndTime { get; set; }

    [SugarColumn(ColumnName = "scheduled_start_time", IsNullable = true)]
    public DateTime? ScheduledStartTime { get; set; }

    [SugarColumn(ColumnName = "scheduled_end_time", IsNullable = true)]
    public DateTime? ScheduledEndTime { get; set; }

    [SugarColumn(ColumnName = "meeting_room_no", IsNullable = true)]
    public string MeetingRoomNo { get; set; }

    [SugarColumn(ColumnName = "rtc_channel_id", IsNullable = true)]
    public string RtcChannelId { get; set; }

    [SugarColumn(ColumnName = "rtc_vendor", IsNullable = true)]
    public string RtcVendor { get; set; }

    [SugarColumn(ColumnName = "audit_doctor_id", IsNullable = true)]
    public long? AuditDoctorId { get; set; }

    [SugarColumn(ColumnName = "audit_time", IsNullable = true)]
    public DateTime? AuditTime { get; set; }

    [SugarColumn(ColumnName = "audit_comment", IsNullable = true)]
    public string AuditComment { get; set; }

    [SugarColumn(ColumnName = "close_reason", IsNullable = true)]
    public string CloseReason { get; set; }
}