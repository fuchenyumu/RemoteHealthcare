
namespace PurestAdmin.Application.RcPatientCaseServices.Dtos;

/// <summary>
/// 患者快照信息（列表）
/// </summary>
public class RcPatientCaseSummaryOutput
{
    public long Id { get; set; }

    public string PatientName { get; set; }

    public string PatientSex { get; set; }

    public string PatientType { get; set; }

    public string PatientTypeLabel { get; set; }

    public string Phone { get; set; }

    public decimal? GestationalWeeks { get; set; }

    public int? ChildAgeMonths { get; set; }

    public string DataSource { get; set; }

    public DateTime CreateTime { get; set; }
}

/// <summary>
/// 患者快照详情
/// </summary>
public class RcPatientCaseDetailOutput : RcPatientCaseSummaryOutput
{
    public string Remark { get; set; }

    public string HisPatientId { get; set; }

    public string HisVisitId { get; set; }

    public DateTime? PatientBirth { get; set; }

    public string IdCard { get; set; }

    public string ContactAddress { get; set; }

    public string MedicalSummary { get; set; }

    public string Allergies { get; set; }

    public decimal? GestationalWeekValue { get; set; }
}
