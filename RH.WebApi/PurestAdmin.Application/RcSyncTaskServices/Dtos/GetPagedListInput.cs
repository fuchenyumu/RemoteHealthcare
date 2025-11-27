
namespace PurestAdmin.Application.RcSyncTaskServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    public string TaskType { get; set; }

    public string TaskStatus { get; set; }

    public string Keyword { get; set; }
}
