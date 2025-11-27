
namespace PurestAdmin.Application.RcConsultationReportServices.Dtos;

/// <summary>
/// 会诊报告详情
/// </summary>
public class RcConsultationReportDetailOutput
{
    public long Id { get; set; }

    public long ConsultationId { get; set; }

    public string Summary { get; set; }

    public string Diagnosis { get; set; }

    public string TreatmentAdvice { get; set; }

    public string FollowUpPlan { get; set; }

    public string ReportStatus { get; set; }

    public string ReportStatusLabel { get; set; }

    public long? SignerId { get; set; }

    public string SignerName { get; set; }

    public DateTime? SignedTime { get; set; }

    public long? SignatureFileId { get; set; }

    public string SignatureFileUrl { get; set; }

    public string Remark { get; set; }
}
