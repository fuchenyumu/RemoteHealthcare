using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 会诊资料附件
/// </summary>
[SugarTable("PUREST_RC_CONSULTATION_ATTACHMENT")]
public class RcConsultationAttachmentEntity : BaseEntity
{
    [SugarColumn(ColumnName = "consultation_id")]
    public long ConsultationId { get; set; }

    [SugarColumn(ColumnName = "file_id", IsNullable = true)]
    public long? FileId { get; set; }

    [SugarColumn(ColumnName = "external_uri", IsNullable = true)]
    public string ExternalUri { get; set; }

    [SugarColumn(ColumnName = "attachment_type")]
    public string AttachmentType { get; set; }

    [SugarColumn(ColumnName = "source_system")]
    public string SourceSystem { get; set; }

    [SugarColumn(ColumnName = "description", IsNullable = true)]
    public string Description { get; set; }
}