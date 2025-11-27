using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 会诊结束
/// </summary>
public class FinishRcConsultationInput
{
    [Required]
    public long ConsultationId { get; set; }

    [MaxLength(500)]
    public string Summary { get; set; }
}

