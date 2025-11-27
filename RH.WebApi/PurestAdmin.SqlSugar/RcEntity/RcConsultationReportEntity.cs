using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 会诊结论报告
/// </summary>
[SugarTable("PUREST_RC_CONSULTATION_REPORT")]
public class RcConsultationReportEntity : BaseEntity
{
    [SugarColumn(ColumnName = "consultation_id")]
    public long ConsultationId { get; set; }

    [SugarColumn(ColumnName = "summary")]
    public string Summary { get; set; }

    [SugarColumn(ColumnName = "diagnosis", IsNullable = true)]
    public string Diagnosis { get; set; }

    [SugarColumn(ColumnName = "treatment_advice", IsNullable = true)]
    public string TreatmentAdvice { get; set; }

    [SugarColumn(ColumnName = "follow_up_plan", IsNullable = true)]
    public string FollowUpPlan { get; set; }

    [SugarColumn(ColumnName = "signer_id", IsNullable = true)]
    public long? SignerId { get; set; }

    [SugarColumn(ColumnName = "signed_time", IsNullable = true)]
    public DateTime? SignedTime { get; set; }

    [SugarColumn(ColumnName = "signature_file_id", IsNullable = true)]
    public long? SignatureFileId { get; set; }

    [SugarColumn(ColumnName = "report_status")]
    public string ReportStatus { get; set; }
}