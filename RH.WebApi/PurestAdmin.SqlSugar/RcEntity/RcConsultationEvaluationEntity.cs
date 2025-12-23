using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 远程会诊患者评价表
/// </summary>
[SugarTable("PUREST_RC_CONSULTATION_EVALUATION")]
public class RcConsultationEvaluationEntity : BaseEntity
{
    [SugarColumn(ColumnName = "consultation_id")]
    public long ConsultationId { get; set; }

    [SugarColumn(ColumnName = "patient_name", IsNullable = true)]
    public string PatientName { get; set; }

    /// <summary>
    /// 评分 1-5
    /// </summary>
    [SugarColumn(ColumnName = "score")]
    public int Score { get; set; }

    /// <summary>
    /// 评价标签，逗号分隔
    /// </summary>
    [SugarColumn(ColumnName = "tags", IsNullable = true)]
    public string Tags { get; set; }

    /// <summary>
    /// 评价内容
    /// </summary>
    [SugarColumn(ColumnName = "content", IsNullable = true)]
    public string Content { get; set; }

    /// <summary>
    /// 是否匿名
    /// </summary>
    [SugarColumn(ColumnName = "is_anonymous")]
    public bool IsAnonymous { get; set; }
}






