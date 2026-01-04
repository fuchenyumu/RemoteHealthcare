using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using PurestAdmin.Application.RcConsultationShareServices.Dtos;
using PurestAdmin.Application.RcRtcServices.Dtos;
using PurestAdmin.Application.RemoteHealthcare.Sfu;
using PurestAdmin.Core.Cache;
using PurestAdmin.Core.ExceptionExtensions;
using PurestAdmin.Multiplex.Contracts.Consts;
using PurestAdmin.Multiplex.Contracts.Enums;
using PurestAdmin.Multiplex.Contracts.IAdminUser;
using PurestAdmin.SqlSugar.Entity;
using PurestAdmin.SqlSugar.RcEntity;
using SqlSugar;

namespace PurestAdmin.Application.RcConsultationShareServices;

/// <summary>
/// 会诊分享链接服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
[Route("api/v1/rc-consultation")]
public class RcConsultationShareService(
    ISqlSugarClient db,
    ICurrentUser currentUser,
    IOptions<SfuOptions> options,
    ISfuApiClient sfuApiClient,
    IAdminCache cache) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly SfuOptions _options = options.Value;
    private readonly ISfuApiClient _sfuApiClient = sfuApiClient;
    private readonly IAdminCache _cache = cache;

    private long CurrentUserId => _currentUser?.Id ?? 0;

    /// <summary>
    /// 创建分享链接
    /// </summary>
    [HttpPost("{consultationId:long}/share")]
    public async Task<ShareLinkOutput> CreateShareLinkAsync(long consultationId, [FromBody] CreateShareLinkInput input)
    {
        // 验证会诊是否存在
        var consultation = await _db.Queryable<RcConsultationEntity>()
            .Where(x => x.Id == consultationId)
            .FirstAsync()
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "会诊不存在");

        // 验证会诊状态
        if (consultation.ConsultationStatus != "IN_PROGRESS" && consultation.ConsultationStatus != "SCHEDULED")
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "只能分享进行中或已排期的会诊");
        }

        // 生成唯一的分享码
        var shareToken = GenerateShareToken(consultationId);

        var shareLink = new RcConsultationShareEntity
        {
            ConsultationId = consultationId,
            ShareToken = shareToken,
            CreatorUserId = CurrentUserId,
            VisitorDisplayName = input.VisitorDisplayName,
            ExpireTime = DateTime.UtcNow.AddHours(input.ExpireHours),
            MaxUsageCount = input.MaxUsageCount,
            UsedCount = 0,
            IsEnabled = true,
            Remark = input.Remark
        };

        await _db.Insertable(shareLink).ExecuteReturnEntityAsync();

        return new ShareLinkOutput
        {
            Id = shareLink.Id,
            ConsultationId = consultationId,
            ShareToken = shareToken,
            ShareUrl = GenerateShareUrl(shareToken),
            ExpireTime = shareLink.ExpireTime,
            MaxUsageCount = shareLink.MaxUsageCount,
            UsedCount = 0,
            IsEnabled = shareLink.IsEnabled,
            CreateTime = shareLink.CreateTime
        };
    }

    /// <summary>
    /// 验证分享链接(无需认证)
    /// </summary>
    [HttpGet("share/validate/{shareToken}")]
    [AllowAnonymous]
    public async Task<ValidateShareTokenOutput> ValidateShareTokenAsync(string shareToken)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
        {
            return new ValidateShareTokenOutput
            {
                IsValid = false,
                ErrorMessage = "分享码不能为空"
            };
        }

        var shareLink = await _db.Queryable<RcConsultationShareEntity>()
            .Where(x => x.ShareToken == shareToken)
            .FirstAsync();

        if (shareLink == null)
        {
            return new ValidateShareTokenOutput
            {
                IsValid = false,
                ErrorMessage = "分享链接不存在"
            };
        }

        // 检查是否启用
        if (!shareLink.IsEnabled)
        {
            return new ValidateShareTokenOutput
            {
                IsValid = false,
                ErrorMessage = "分享链接已禁用"
            };
        }

        // 检查是否过期
        if (shareLink.ExpireTime < DateTime.UtcNow)
        {
            return new ValidateShareTokenOutput
            {
                IsValid = false,
                ErrorMessage = "分享链接已过期"
            };
        }

        // 检查使用次数
        if (shareLink.MaxUsageCount > 0 && shareLink.UsedCount >= shareLink.MaxUsageCount)
        {
            return new ValidateShareTokenOutput
            {
                IsValid = false,
                ErrorMessage = "分享链接使用次数已达上限"
            };
        }

        // 查询会诊信息
        var consultation = await _db.Queryable<RcConsultationEntity, RcPatientCaseEntity>(
            (c, pc) => new object[] { JoinType.Left, c.CaseId == pc.Id })
            .Where((c, pc) => c.Id == shareLink.ConsultationId)
            .Select((c, pc) => new ConsultationInfoOutput
            {
                Id = c.Id,
                PatientName = pc.PatientName,
                Purpose = c.Purpose,
                ConsultationStatus = c.ConsultationStatus,
                ScheduledStartTime = c.ScheduledStartTime,
                MeetingRoomNo = c.MeetingRoomNo
            })
            .FirstAsync();

        if (consultation == null)
        {
            return new ValidateShareTokenOutput
            {
                IsValid = false,
                ErrorMessage = "会诊不存在"
            };
        }

        return new ValidateShareTokenOutput
        {
            IsValid = true,
            ConsultationId = shareLink.ConsultationId,
            Consultation = consultation,
            VisitorDisplayName = shareLink.VisitorDisplayName
        };
    }

    /// <summary>
    /// 通过分享链接获取RTC Token(无需认证)
    /// </summary>
    [HttpPost("share/rtc-token")]
    [AllowAnonymous]
    public async Task<RtcTokenOutput> GetRtcTokenByShareTokenAsync([FromBody] ShareTokenRtcInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ShareToken))
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "分享码不能为空");
        }

        // 验证分享链接
        var validation = await ValidateShareTokenAsync(input.ShareToken);
        if (!validation.IsValid || validation.ConsultationId == null)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, validation.ErrorMessage ?? "分享链接无效");
        }

        var consultationId = validation.ConsultationId.Value;

        // 查询会诊信息
        var consultation = await _db.Queryable<RcConsultationEntity>()
            .Where(x => x.Id == consultationId)
            .FirstAsync()
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "会诊不存在");

        // 确保房间存在
        var roomId = consultation.MeetingRoomNo;
        if (string.IsNullOrWhiteSpace(roomId))
        {
            roomId = $"rc-{consultation.Id}";
            consultation.MeetingRoomNo = roomId;
            await _db.Updateable(consultation)
                .UpdateColumns(x => new { x.MeetingRoomNo })
                .ExecuteCommandAsync();
        }

        // 生成匿名用户标识
        var anonymousUserId = $"anon_{Guid.NewGuid():N}";
        var displayName = !string.IsNullOrWhiteSpace(input.VisitorName)
            ? input.VisitorName.Trim()
            : !string.IsNullOrWhiteSpace(validation.VisitorDisplayName)
                ? validation.VisitorDisplayName
                : "外部专家";

        // 请求SFU Token
        var tokenResponse = await _sfuApiClient.RequestTokenAsync(new SfuTokenRequest(
            roomId,
            consultationId,
            anonymousUserId,
            displayName,
            "EXPERT",
            "")); // 匿名用户不设置host_id

        var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        // 更新使用次数和访问时间
        var shareLink = await _db.Queryable<RcConsultationShareEntity>()
            .Where(x => x.ShareToken == input.ShareToken)
            .FirstAsync();

        if (shareLink != null)
        {
            await _db.Updateable<RcConsultationShareEntity>()
                .SetColumns(x => x.UsedCount, x => x.UsedCount + 1)
                .SetColumnsIF(!shareLink.FirstAccessTime.HasValue,
                    x => x.FirstAccessTime, DateTime.UtcNow)
                .SetColumns(x => x.LastAccessTime, DateTime.UtcNow)
                .Where(x => x.ShareToken == input.ShareToken)
                .ExecuteCommandAsync();
        }

        return new RtcTokenOutput
        {
            RoomId = roomId,
            Token = tokenResponse.Token,
            ExpiresIn = tokenResponse.ExpiresIn,
            Role = "EXPERT",
            DisplayName = displayName,
            UserId = anonymousUserId,
            HostId = "",
            SignalingUrl = BuildSignalingUrl(roomId)
        };
    }

    /// <summary>
    /// 查询会诊的分享链接列表
    /// </summary>
    [HttpGet("{consultationId:long}/share/links")]
    public async Task<List<ShareLinkOutput>> GetShareLinksAsync(long consultationId)
    {
        var links = await _db.Queryable<RcConsultationShareEntity>()
            .Where(x => x.ConsultationId == consultationId)
            .OrderBy(x => x.CreateTime, OrderByType.Desc)
            .ToListAsync();

        return links.Select(link => new ShareLinkOutput
        {
            Id = link.Id,
            ConsultationId = link.ConsultationId,
            ShareToken = link.ShareToken,
            ShareUrl = GenerateShareUrl(link.ShareToken),
            ExpireTime = link.ExpireTime,
            MaxUsageCount = link.MaxUsageCount,
            UsedCount = link.UsedCount,
            IsEnabled = link.IsEnabled,
            CreateTime = link.CreateTime
        }).ToList();
    }

    /// <summary>
    /// 禁用/启用分享链接
    /// </summary>
    [HttpPut("{consultationId:long}/share/{id:long}/toggle")]
    public async Task<bool> ToggleShareLinkAsync(long consultationId, long id)
    {
        var link = await _db.Queryable<RcConsultationShareEntity>()
            .Where(x => x.Id == id && x.ConsultationId == consultationId)
            .FirstAsync()
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "分享链接不存在");

        link.IsEnabled = !link.IsEnabled;
        await _db.Updateable(link)
            .UpdateColumns(x => x.IsEnabled)
            .ExecuteCommandAsync();

        return link.IsEnabled;
    }

    /// <summary>
    /// 删除分享链接
    /// </summary>
    [HttpDelete("{consultationId:long}/share/{id:long}")]
    public async Task<bool> DeleteShareLinkAsync(long consultationId, long id)
    {
        var count = await _db.Deleteable<RcConsultationShareEntity>()
            .Where(x => x.Id == id && x.ConsultationId == consultationId)
            .ExecuteCommandAsync();

        return count > 0;
    }

    #region Helper Methods

    /// <summary>
    /// 生成分享Token
    /// </summary>
    private string GenerateShareToken(long consultationId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = Guid.NewGuid().ToString("N").Substring(0, 8);
        var signature = ComputeSignature(consultationId, timestamp, random);
        return $"rc-share-{consultationId}-{timestamp}-{random}-{signature}";
    }

    /// <summary>
    /// 计算签名
    /// </summary>
    private string ComputeSignature(long consultationId, long timestamp, string random)
    {
        var data = $"{consultationId}|{timestamp}|{random}";
        // 使用固定密钥用于签名验证
        var secretKey = "remote-healthcare-share-secret-key-2024";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant().Substring(0, 16);
    }

    /// <summary>
    /// 生成分享URL
    /// </summary>
    private string GenerateShareUrl(string shareToken)
    {
        // 使用前端Web应用基础地址，而不是SFU服务地址
        var baseUrl = !string.IsNullOrWhiteSpace(_options.WebBaseUrl)
            ? _options.WebBaseUrl.TrimEnd('/')
            : "http://localhost:3000";
        // 前端使用 hash 路由模式，需要添加 # 符号
        return $"{baseUrl}/#/share/consultation/{shareToken}";
    }

    /// <summary>
    /// 构建WebSocket信令URL
    /// </summary>
    private string BuildSignalingUrl(string roomId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.SignalingBaseUrl)
            ? _options.BaseUrl.Replace("http", "ws").TrimEnd('/') + "/ws/medical"
            : _options.SignalingBaseUrl;
        return $"{baseUrl.TrimEnd('/')}/{roomId}";
    }

    #endregion
}
