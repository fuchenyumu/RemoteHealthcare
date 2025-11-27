using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 患者资料打包记录
/// </summary>
[SugarTable("PUREST_RC_PATIENT_PACK")]
public class RcPatientPackEntity : BaseEntity
{
    [SugarColumn(ColumnName = "case_id")]
    public long CaseId { get; set; }

    [SugarColumn(ColumnName = "pack_no")]
    public string PackNo { get; set; }

    [SugarColumn(ColumnName = "pack_status")]
    public string PackStatus { get; set; }

    [SugarColumn(ColumnName = "expire_time", IsNullable = true)]
    public DateTime? ExpireTime { get; set; }

    [SugarColumn(ColumnName = "file_count")]
    public int FileCount { get; set; }

    [SugarColumn(ColumnName = "total_size")]
    public long TotalSize { get; set; }

    [SugarColumn(ColumnName = "sync_task_id", IsNullable = true)]
    public long? SyncTaskId { get; set; }
}