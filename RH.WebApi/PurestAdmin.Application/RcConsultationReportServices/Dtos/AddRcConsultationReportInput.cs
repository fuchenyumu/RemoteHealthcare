
using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationReportServices.Dtos;

/// <summary>
/// 创建或更新会诊报告内容
/// </summary>
public class AddRcConsultationReportInput
{
    public string Remark { get; set; }

    [Required]
    public long ConsultationId { get; set; }

    [Required]
    public string Summary { get; set; }

    public string Diagnosis { get; set; }

    public string TreatmentAdvice { get; set; }

    public string FollowUpPlan { get; set; }

    [MaxLength(20)]
    public string ReportStatus { get; set; }
}
