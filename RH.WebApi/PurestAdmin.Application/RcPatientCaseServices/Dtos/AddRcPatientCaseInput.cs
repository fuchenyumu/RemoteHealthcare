
using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcPatientCaseServices.Dtos;

/// <summary>
/// 新增患者快照
/// </summary>
public class AddRcPatientCaseInput
{
    public string Remark { get; set; }

    [MaxLength(50)]
    public string HisPatientId { get; set; }

    [MaxLength(50)]
    public string HisVisitId { get; set; }

    [Required, MaxLength(40)]
    public string PatientName { get; set; }

    [Required, MaxLength(2)]
    public string PatientSex { get; set; }

    public DateTime? PatientBirth { get; set; }

    [Required, MaxLength(20)]
    public string PatientType { get; set; }

    [MaxLength(64)]
    public string IdCard { get; set; }

    [MaxLength(30)]
    public string Phone { get; set; }

    [MaxLength(200)]
    public string ContactAddress { get; set; }

    public string MedicalSummary { get; set; }

    public string Allergies { get; set; }

    public decimal? GestationalWeeks { get; set; }

    public int? ChildAgeMonths { get; set; }

    [Required, MaxLength(30)]
    public string DataSource { get; set; }
}
