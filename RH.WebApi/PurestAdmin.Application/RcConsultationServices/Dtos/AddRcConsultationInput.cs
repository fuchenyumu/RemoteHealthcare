using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 创建远程会诊申请
/// </summary>
public class AddRcConsultationInput
{
    public string Remark { get; set; }

    [Required]
    public long CaseId { get; set; }

    public long? PackId { get; set; }

    [Required]
    public long ApplyOrgId { get; set; }

    [Required]
    public long ApplyDoctorId { get; set; }

    public long? TargetOrgId { get; set; }

    [MaxLength(50)]
    public string TargetDepartment { get; set; }

    public long? TargetExpertId { get; set; }

    [Required]
    public string Purpose { get; set; }

    [Required, MaxLength(20)]
    public string EmergencyLevel { get; set; }

    public DateTime? DesiredStartTime { get; set; }

    public DateTime? DesiredEndTime { get; set; }
}

