using PurestAdmin.Multiplex;

namespace PurestAdmin.Application.RcConsultationEvaluationServices.Dtos;

public class GetRcEvaluationPagedListInput : PaginationParams
{
    public long? ConsultationId { get; set; }
}






