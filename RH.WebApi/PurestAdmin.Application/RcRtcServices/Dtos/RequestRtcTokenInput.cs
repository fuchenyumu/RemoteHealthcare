namespace PurestAdmin.Application.RcRtcServices.Dtos;

public class RequestRtcTokenInput
{
    /// <summary>
    /// 是否强制刷新令牌，即使缓存未过期。
    /// </summary>
    public bool ForceRefresh { get; set; }
}

