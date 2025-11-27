
using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationReportServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    [Required]
    public long ConsultationId { get; set; }

    public string ReportStatus { get; set; }
}
