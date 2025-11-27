using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 远程会诊患者快照（同步院内主数据）
/// </summary>
[SugarTable("PUREST_RC_PATIENT_CASE")]
public class RcPatientCaseEntity : BaseEntity
{
    [SugarColumn(ColumnName = "his_patient_id", IsNullable = true)]
    public string HisPatientId { get; set; }

    [SugarColumn(ColumnName = "his_visit_id", IsNullable = true)]
    public string HisVisitId { get; set; }

    [SugarColumn(ColumnName = "patient_name")]
    public string PatientName { get; set; }

    [SugarColumn(ColumnName = "patient_sex")]
    public string PatientSex { get; set; }

    [SugarColumn(ColumnName = "patient_birth", IsNullable = true)]
    public DateTime? PatientBirth { get; set; }

    [SugarColumn(ColumnName = "patient_type")]
    public string PatientType { get; set; }

    [SugarColumn(ColumnName = "id_card", IsNullable = true)]
    public string IdCard { get; set; }

    [SugarColumn(ColumnName = "phone", IsNullable = true)]
    public string Phone { get; set; }

    [SugarColumn(ColumnName = "contact_address", IsNullable = true)]
    public string ContactAddress { get; set; }

    [SugarColumn(ColumnName = "medical_summary", IsNullable = true)]
    public string MedicalSummary { get; set; }

    [SugarColumn(ColumnName = "allergies", IsNullable = true)]
    public string Allergies { get; set; }

    [SugarColumn(ColumnName = "gestational_weeks", IsNullable = true)]
    public decimal? GestationalWeeks { get; set; }

    [SugarColumn(ColumnName = "child_age_months", IsNullable = true)]
    public int? ChildAgeMonths { get; set; }

    [SugarColumn(ColumnName = "data_source")]
    public string DataSource { get; set; }
}