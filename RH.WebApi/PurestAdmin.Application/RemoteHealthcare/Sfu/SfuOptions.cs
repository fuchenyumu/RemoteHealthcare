using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RemoteHealthcare.Sfu;

/// <summary>
/// 配置 Python SFU 服务访问地址及令牌策略。
/// </summary>
public class SfuOptions
{
    /// <summary>
    /// SFU 服务基础地址，例如 http://127.0.0.1:8000
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = "http://127.0.0.1:8000";

    /// <summary>
    /// 前端Web应用基础地址，例如 http://localhost:3000
    /// 用于生成分享链接
    /// </summary>
    public string? WebBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// 颁发 Token 的接口路径，默认 /api/health/token
    /// </summary>
    [Required]
    public string TokenEndpoint { get; set; } = "/api/health/token";

    /// <summary>
    /// 房间列表接口路径，默认 /api/health/rooms
    /// </summary>
    [Required]
    public string RoomsEndpoint { get; set; } = "/api/health/rooms";

    /// <summary>
    /// 服务状态接口路径，默认 /api/health/status
    /// </summary>
    [Required]
    public string StatusEndpoint { get; set; } = "/api/health/status";

    /// <summary>
    /// WebSocket 信令基础地址，默认 ws://127.0.0.1:8000/ws/medical
    /// </summary>
    public string? SignalingBaseUrl { get; set; } = "ws://127.0.0.1:8000/ws/medical";

    /// <summary>
    /// 缓存 Token 时距离过期预留的秒数，默认 30 秒
    /// </summary>
    public int TokenCacheBufferSeconds { get; set; } = 30;
}

