
namespace PurestAdmin.Application.RcPatientPackServices.Dtos;

/// <summary>
/// 患者资料打包结果
/// </summary>
public class RcPatientPackOutput
{
    public long Id { get; set; }

    public long CaseId { get; set; }

    public string PackNo { get; set; }

    public string PackStatus { get; set; }

    public string PackStatusLabel { get; set; }

    public DateTime? ExpireTime { get; set; }

    public int FileCount { get; set; }

    public long TotalSize { get; set; }

    public long? SyncTaskId { get; set; }

    public string Remark { get; set; }

    public DateTime CreateTime { get; set; }
}
