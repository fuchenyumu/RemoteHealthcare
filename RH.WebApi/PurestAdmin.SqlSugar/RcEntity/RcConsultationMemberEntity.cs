using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 会诊参与成员
/// </summary>
[SugarTable("PUREST_RC_CONSULTATION_MEMBER")]
public class RcConsultationMemberEntity : BaseEntity
{
    [SugarColumn(ColumnName = "consultation_id")]
    public long ConsultationId { get; set; }

    [SugarColumn(ColumnName = "user_id")]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "org_id")]
    public long OrgId { get; set; }

    [SugarColumn(ColumnName = "role_code")]
    public string RoleCode { get; set; }

    [SugarColumn(ColumnName = "join_status")]
    public string JoinStatus { get; set; }

    [SugarColumn(ColumnName = "join_time", IsNullable = true)]
    public DateTime? JoinTime { get; set; }

    [SugarColumn(ColumnName = "leave_time", IsNullable = true)]
    public DateTime? LeaveTime { get; set; }
}