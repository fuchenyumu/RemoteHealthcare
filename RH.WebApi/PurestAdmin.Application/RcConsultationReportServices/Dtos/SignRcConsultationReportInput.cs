using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationReportServices.Dtos;

/// <summary>
/// 报告签署请求
/// </summary>
public class SignRcConsultationReportInput
{
    [Required]
    public long ConsultationId { get; set; }

    [Required]
    public long SignerId { get; set; }

    public long? SignatureFileId { get; set; }

    public string Remark { get; set; }
}

