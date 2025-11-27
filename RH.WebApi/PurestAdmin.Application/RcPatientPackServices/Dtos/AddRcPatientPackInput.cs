
using System.ComponentModel.DataAnnotations;
using PurestAdmin.Multiplex.Contracts.Consts;

namespace PurestAdmin.Application.RcPatientPackServices.Dtos;

/// <summary>
/// 创建打包任务
/// </summary>
public class AddRcPatientPackInput
{
    [Required]
    public long CaseId { get; set; }

    [MaxLength(20)]
    public string PackStatus { get; set; } = RemoteHealthcareConsts.PackStatus.Pending;

    public DateTime? ExpireTime { get; set; }

    public long? SyncTaskId { get; set; }

    [MaxLength(1000)]
    public string Remark { get; set; }
}
