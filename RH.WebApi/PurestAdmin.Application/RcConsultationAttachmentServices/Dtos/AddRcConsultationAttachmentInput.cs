
using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationAttachmentServices.Dtos;

/// <summary>
/// 新增会诊附件
/// </summary>
public class AddRcConsultationAttachmentInput
{
    public string Remark { get; set; }

    [Required]
    public long ConsultationId { get; set; }

    public long? FileId { get; set; }

    [MaxLength(255)]
    public string ExternalUri { get; set; }

    [Required, MaxLength(20)]
    public string AttachmentType { get; set; }

    [Required, MaxLength(30)]
    public string SourceSystem { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }
}
