namespace PurestAdmin.Application.RcConsultationShareServices.Dtos;

/// <summary>
/// 通过分享Token获取RTC Token输入
/// </summary>
public class ShareTokenRtcInput
{
    /// <summary>
    /// 分享Token
    /// </summary>
    public string ShareToken { get; set; }

    /// <summary>
    /// 访客显示名称
    /// </summary>
    public string? VisitorName { get; set; }
}
