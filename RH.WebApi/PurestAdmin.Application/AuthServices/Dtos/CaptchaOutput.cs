// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace PurestAdmin.Application.AuthServices.Dtos;

/// <summary>
/// 验证码输出
/// </summary>
public class CaptchaOutput
{
    /// <summary>
    /// 验证码ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 验证码图片（Base64）
    /// </summary>
    public string Img { get; set; }

    /// <summary>
    /// 验证码过期时间（Unix时间戳）
    /// </summary>
    public long ExpiresAt { get; set; }
}
