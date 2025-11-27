
namespace PurestAdmin.Application.RcConsultationMemberServices.Dtos;

/// <summary>
/// 会诊成员信息
/// </summary>
public class RcConsultationMemberOutput
{
    public long Id { get; set; }

    public long ConsultationId { get; set; }

    public long UserId { get; set; }

    public string UserName { get; set; }

    public long OrgId { get; set; }

    public string OrgName { get; set; }

    public string RoleCode { get; set; }

    public string RoleName { get; set; }

    public string JoinStatus { get; set; }

    public string JoinStatusLabel { get; set; }

    public DateTime? JoinTime { get; set; }

    public DateTime? LeaveTime { get; set; }

    public string Remark { get; set; }
}
