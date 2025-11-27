
using System.ComponentModel.DataAnnotations;
using PurestAdmin.Multiplex.Contracts.Consts;

namespace PurestAdmin.Application.RcPatientPackServices.Dtos;

public class PutRcPatientPackInput
{
    [Required]
    public long CaseId { get; set; }

    [Required, MaxLength(20)]
    public string PackStatus { get; set; } = RemoteHealthcareConsts.PackStatus.Pending;

    public DateTime? ExpireTime { get; set; }

    public long? SyncTaskId { get; set; }

    [MaxLength(1000)]
    public string Remark { get; set; }
}
