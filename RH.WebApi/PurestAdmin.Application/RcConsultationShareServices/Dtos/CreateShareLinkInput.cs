namespace PurestAdmin.Application.RcConsultationShareServices.Dtos;

/// <summary>
/// 创建分享链接输入
/// </summary>
public class CreateShareLinkInput
{
    /// <summary>
    /// 会诊ID
    /// </summary>
    public long ConsultationId { get; set; }

    /// <summary>
    /// 过期小时数(默认24小时)
    /// </summary>
    public int ExpireHours { get; set; } = 24;

    /// <summary>
    /// 最大使用次数(0表示不限制)
    /// </summary>
    public int MaxUsageCount { get; set; } = 0;

    /// <summary>
    /// 访客默认显示名称
    /// </summary>
    public string? VisitorDisplayName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
