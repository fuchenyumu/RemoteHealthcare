
using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcSyncTaskServices.Dtos;

/// <summary>
/// 新增同步任务
/// </summary>
public class AddRcSyncTaskInput
{
    public string Remark { get; set; }

    [Required, MaxLength(40)]
    public string TaskNo { get; set; }

    [Required, MaxLength(20)]
    public string TaskType { get; set; }

    [Required, MaxLength(20)]
    public string TaskStatus { get; set; }

    public object RequestPayload { get; set; }

    public object ResponsePayload { get; set; }

    public DateTime? CompletedTime { get; set; }

    [Range(0, 100)]
    public int RetryCount { get; set; }
}
