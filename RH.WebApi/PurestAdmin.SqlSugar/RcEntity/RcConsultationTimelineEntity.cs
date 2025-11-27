using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 会诊流程轨迹
/// </summary>
[SugarTable("PUREST_RC_CONSULTATION_TIMELINE")]
public class RcConsultationTimelineEntity : BaseEntity
{
    [SugarColumn(ColumnName = "consultation_id")]
    public long ConsultationId { get; set; }

    [SugarColumn(ColumnName = "event_code")]
    public string EventCode { get; set; }

    [SugarColumn(ColumnName = "event_content", IsNullable = true)]
    public string EventContent { get; set; }

    [SugarColumn(ColumnName = "snapshot", ColumnDataType = "jsonb", IsNullable = true, IsJson = true)]
    public object Snapshot { get; set; }
}