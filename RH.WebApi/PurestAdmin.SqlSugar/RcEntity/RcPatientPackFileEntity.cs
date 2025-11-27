using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 资料包附件
/// </summary>
[SugarTable("PUREST_RC_PATIENT_PACK_FILE")]
public class RcPatientPackFileEntity : BaseEntity
{
    [SugarColumn(ColumnName = "pack_id")]
    public long PackId { get; set; }

    [SugarColumn(ColumnName = "file_id")]
    public long FileId { get; set; }

    [SugarColumn(ColumnName = "file_name", Length = 255)]
    public string FileName { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [SugarColumn(ColumnName = "file_size")]
    public long FileSize { get; set; }
}

