namespace PurestAdmin.Application.RcConsultationShareServices.Dtos;

/// <summary>
/// 分享链接输出
/// </summary>
public class ShareLinkOutput
{
    /// <summary>
    /// 分享链接ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 会诊ID
    /// </summary>
    public long ConsultationId { get; set; }

    /// <summary>
    /// 分享码
    /// </summary>
    public string ShareToken { get; set; }

    /// <summary>
    /// 完整的分享URL
    /// </summary>
    public string ShareUrl { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpireTime { get; set; }

    /// <summary>
    /// 最大使用次数
    /// </summary>
    public int MaxUsageCount { get; set; }

    /// <summary>
    /// 已使用次数
    /// </summary>
    public int UsedCount { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
}
