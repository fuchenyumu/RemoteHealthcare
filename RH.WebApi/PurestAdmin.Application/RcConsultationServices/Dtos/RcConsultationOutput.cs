
using System;
using System.Collections.Generic;
using PurestAdmin.Application.RcConsultationAttachmentServices.Dtos;
using PurestAdmin.Application.RcConsultationMemberServices.Dtos;
using PurestAdmin.Application.RcConsultationReportServices.Dtos;
using PurestAdmin.Application.RcConsultationTimelineServices.Dtos;
using PurestAdmin.Application.RcPatientCaseServices.Dtos;
using PurestAdmin.Application.RcPatientPackServices.Dtos;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 会诊列表/详情公共字段
/// </summary>
public class RcConsultationSummaryOutput
{
    public long Id { get; set; }

    public long CaseId { get; set; }

    public long? PackId { get; set; }

    public string PackNo { get; set; }

    public string PatientName { get; set; }

    public string PatientType { get; set; }

    public string PatientTypeLabel { get; set; }

    public long ApplyOrgId { get; set; }

    public string ApplyOrgName { get; set; }

    public long ApplyDoctorId { get; set; }

    public string ApplyDoctorName { get; set; }

    public long? TargetOrgId { get; set; }

    public string TargetOrgName { get; set; }

    public string TargetDepartment { get; set; }

    public long? TargetExpertId { get; set; }

    public string TargetExpertName { get; set; }

    public string Purpose { get; set; }

    public string EmergencyLevel { get; set; }

    public string EmergencyLevelLabel { get; set; }

    public string ConsultationStatus { get; set; }

    public string ConsultationStatusLabel { get; set; }

    public DateTime? DesiredStartTime { get; set; }

    public DateTime? DesiredEndTime { get; set; }

    public DateTime? ScheduledStartTime { get; set; }

    public DateTime? ScheduledEndTime { get; set; }

    public string MeetingRoomNo { get; set; }

    public string RtcChannelId { get; set; }

    public string RtcVendor { get; set; }

    public long? AuditDoctorId { get; set; }

    public string AuditDoctorName { get; set; }

    public DateTime? AuditTime { get; set; }

    public string AuditComment { get; set; }

    public string CloseReason { get; set; }

    public long CreateBy { get; set; }

    public string CreateByName { get; set; }

    public DateTime CreateTime { get; set; }

    public long? UpdateBy { get; set; }

    public DateTime? UpdateTime { get; set; }

    public string Remark { get; set; }
}

/// <summary>
/// 会诊详情输出
/// </summary>
public class RcConsultationDetailOutput : RcConsultationSummaryOutput
{
    /// <summary>
    /// H5 评价链接（完整的 URL，用于生成二维码）
    /// </summary>
    public string EvaluationUrl { get; set; }

    public RcPatientCaseDetailOutput PatientCase { get; set; }

    public RcPatientPackOutput PatientPack { get; set; }

    public IReadOnlyList<RcConsultationMemberOutput> Members { get; set; } = Array.Empty<RcConsultationMemberOutput>();

    public IReadOnlyList<RcConsultationAttachmentOutput> Attachments { get; set; } = Array.Empty<RcConsultationAttachmentOutput>();

    public IReadOnlyList<RcConsultationTimelineOutput> Timeline { get; set; } = Array.Empty<RcConsultationTimelineOutput>();

    public RcConsultationReportDetailOutput Report { get; set; }
}
