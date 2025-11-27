namespace PurestAdmin.Application.RcPatientPackServices.Dtos;

/// <summary>
/// 资料包附件输出
/// </summary>
public class RcPatientPackFileOutput
{
    public long Id { get; set; }

    public long PackId { get; set; }

    public long FileId { get; set; }

    public string FileName { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    public DateTime CreateTime { get; set; }
}

