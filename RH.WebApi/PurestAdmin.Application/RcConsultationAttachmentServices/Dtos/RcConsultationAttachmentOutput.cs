
namespace PurestAdmin.Application.RcConsultationAttachmentServices.Dtos;

/// <summary>
/// 会诊附件信息
/// </summary>
public class RcConsultationAttachmentOutput
{
    public long Id { get; set; }

    public long ConsultationId { get; set; }

    public long? FileId { get; set; }

    public string FileName { get; set; }

    public string ExternalUri { get; set; }

    public string AttachmentType { get; set; }

    public string AttachmentTypeLabel { get; set; }

    public string SourceSystem { get; set; }

    public string SourceSystemLabel { get; set; }

    public string Description { get; set; }

    public DateTime CreateTime { get; set; }
}
