
namespace PurestAdmin.Application.RcSyncTaskServices.Dtos;

public class RcSyncTaskOutput
{
    public long Id { get; set; }

    public string TaskNo { get; set; }

    public string TaskType { get; set; }

    public string TaskTypeLabel { get; set; }

    public string TaskStatus { get; set; }

    public string TaskStatusLabel { get; set; }

    public object RequestPayload { get; set; }

    public object ResponsePayload { get; set; }

    public DateTime? CompletedTime { get; set; }

    public int RetryCount { get; set; }

    public string Remark { get; set; }

    public DateTime CreateTime { get; set; }
}
