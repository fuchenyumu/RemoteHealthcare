
using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationMemberServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    [Required]
    public long ConsultationId { get; set; }

    public string RoleCode { get; set; }

    public string JoinStatus { get; set; }
}
