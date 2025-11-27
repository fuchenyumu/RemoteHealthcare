using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 会诊审核入参
/// </summary>
public class AuditRcConsultationInput
{

    [Required]
    public bool Approved { get; set; }

    [MaxLength(500)]
    public string Comment { get; set; }
}

