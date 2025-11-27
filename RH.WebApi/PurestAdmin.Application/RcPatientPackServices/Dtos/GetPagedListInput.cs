
namespace PurestAdmin.Application.RcPatientPackServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    public long? CaseId { get; set; }

    public string PackStatus { get; set; }

    public string Keyword { get; set; }
}
