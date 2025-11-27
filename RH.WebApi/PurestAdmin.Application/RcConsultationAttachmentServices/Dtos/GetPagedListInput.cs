
namespace PurestAdmin.Application.RcConsultationAttachmentServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    public long ConsultationId { get; set; }

    public string AttachmentType { get; set; }
}
