using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 关闭会诊请求
/// </summary>
public class CloseRcConsultationInput
{
    [Required]
    public long ConsultationId { get; set; }

    [Required]
    public string CloseReason { get; set; }
}

