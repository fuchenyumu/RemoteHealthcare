using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 远程会诊同步任务
/// </summary>
[SugarTable("PUREST_RC_SYNC_TASK")]
public class RcSyncTaskEntity : BaseEntity
{
    [SugarColumn(ColumnName = "task_no")]
    public string TaskNo { get; set; }

    [SugarColumn(ColumnName = "task_type")]
    public string TaskType { get; set; }

    [SugarColumn(ColumnName = "task_status")]
    public string TaskStatus { get; set; }

    [SugarColumn(ColumnName = "request_payload", ColumnDataType = "jsonb", IsNullable = true, IsJson = true)]
    public object RequestPayload { get; set; }

    [SugarColumn(ColumnName = "response_payload", ColumnDataType = "jsonb", IsNullable = true, IsJson = true)]
    public object ResponsePayload { get; set; }

    [SugarColumn(ColumnName = "completed_time", IsNullable = true)]
    public DateTime? CompletedTime { get; set; }

    [SugarColumn(ColumnName = "retry_count")]
    public int RetryCount { get; set; }
}