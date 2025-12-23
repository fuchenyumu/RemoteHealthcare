
using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationTimelineServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    [Required]
    public long ConsultationId { get; set; }

    public string EventCode { get; set; }
}
