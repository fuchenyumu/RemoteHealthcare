using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 会诊开始
/// </summary>
public class StartRcConsultationInput
{
    [Required]
    public long ConsultationId { get; set; }
}

