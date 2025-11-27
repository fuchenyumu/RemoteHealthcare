
namespace PurestAdmin.Application.RcConsultationMemberServices.Dtos;

/// <summary>
/// 添加会诊成员
/// </summary>
public class AddRcConsultationMemberInput
{
    public string Remark { get; set; }

    [Required]
    public long ConsultationId { get; set; }

    [Required]
    public long UserId { get; set; }

    [Required]
    public long OrgId { get; set; }

    [Required, MaxLength(20)]
    public string RoleCode { get; set; }

    [Required, MaxLength(20)]
    public string JoinStatus { get; set; }

    public DateTime? JoinTime { get; set; }

    public DateTime? LeaveTime { get; set; }
}
