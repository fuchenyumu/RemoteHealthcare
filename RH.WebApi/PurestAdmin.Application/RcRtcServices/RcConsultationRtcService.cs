using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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

namespace PurestAdmin.Application.RcRtcServices;

/// <summary>
/// 会诊音视频（RTC）服务，负责代理 Python SFU 的接口
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
[Route("api/v1/rc-rtc")]
public class RcConsultationRtcService(
    ISqlSugarClient db,
    ICurrentUser currentUser,
    ISfuApiClient sfuApiClient,
    IOptions<SfuOptions> options,
    IAdminCache cache) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ISfuApiClient _sfuApiClient = sfuApiClient;
    private readonly SfuOptions _options = options.Value;
    private readonly IAdminCache _cache = cache;

    private long CurrentUserId => _currentUser?.Id ?? 0;

    /// <summary>
    /// 获取会诊的 WebRTC Token
    /// </summary>
    [HttpPost("{consultationId:long}/token")]
    public async Task<RtcTokenOutput> IssueTokenAsync(long consultationId, [FromBody] RequestRtcTokenInput? input)
    {
        var consultation = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == consultationId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var roomId = await EnsureMeetingRoomAsync(consultation);
        var displayName = await ResolveUserNameAsync(CurrentUserId);
        var role = await ResolveParticipantRoleAsync(consultationId, CurrentUserId);
        var hostId = await ResolveHostIdAsync(consultation);

        var cacheKey = $"rtc:token:{consultationId}:{CurrentUserId}";
        var bufferSeconds = Math.Max(5, _options.TokenCacheBufferSeconds);
        if (!(input?.ForceRefresh ?? false))
        {
            var cached = _cache.Get<RtcTokenCacheItem>(cacheKey);
            if (cached != null && cached.ExpiresAt > DateTime.UtcNow.AddSeconds(bufferSeconds))
            {
                return BuildTokenOutput(roomId, cached.Token, cached.ExpiresAt, cached.Role, cached.DisplayName, cached.HostId);
            }
        }

        var tokenResponse = await _sfuApiClient.RequestTokenAsync(new SfuTokenRequest(
            roomId,
            consultationId,
            CurrentUserId.ToString(),
            displayName,
            role,
            hostId));

        var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        var cacheItem = new RtcTokenCacheItem(tokenResponse.Token, expiresAt, role, displayName, hostId);
        var cacheTtl = TimeSpan.FromSeconds(Math.Max(5, tokenResponse.ExpiresIn - bufferSeconds));
        _cache.Set(cacheKey, cacheItem, cacheTtl);

        return BuildTokenOutput(roomId, tokenResponse.Token, expiresAt, role, displayName, hostId);
    }

    /// <summary>
    /// 查询 SFU 房间列表
    /// </summary>
    [HttpGet("rooms")]
    public async Task<IReadOnlyList<RtcRoomStatusOutput>> GetRoomsAsync([FromQuery] RtcRoomQueryInput input)
    {
        var rooms = await _sfuApiClient.GetRoomsAsync();
        if (input?.ConsultationId is long consultationId)
        {
            rooms = rooms.Where(x => x.ConsultationId == consultationId).ToList();
        }

        return rooms.Select(MapRoom).ToList();
    }

    /// <summary>
    /// 查询指定房间详情
    /// </summary>
    [HttpGet("rooms/{roomId}")]
    public async Task<RtcRoomStatusOutput> GetRoomAsync(string roomId)
    {
        var room = await _sfuApiClient.GetRoomAsync(roomId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "房间不存在或已关闭");
        return MapRoom(room);
    }

    /// <summary>
    /// 查询 SFU 服务运行状态
    /// </summary>
    [HttpGet("status")]
    public async Task<RtcServiceStatusOutput> GetStatusAsync()
    {
        var status = await _sfuApiClient.GetServiceStatusAsync();
        return new RtcServiceStatusOutput
        {
            Status = status.Status,
            App = status.App,
            RoomCount = status.RoomCount,
            ParticipantCount = status.ParticipantCount,
            StartedAt = status.StartedAt,
            UptimeSeconds = status.UptimeSeconds
        };
    }

    #region helpers

    private async Task<string> ResolveUserNameAsync(long userId)
    {
        if (userId <= 0)
        {
            return "未知用户";
        }

        if (_currentUser?.Self != null && _currentUser.Self.Id == userId)
        {
            return _currentUser.Self.Name ?? $"用户{userId}";
        }

        var user = await _db.Queryable<UserEntity>().FirstAsync(x => x.Id == userId);
        return user?.Name ?? $"用户{userId}";
    }

    private async Task<string> ResolveParticipantRoleAsync(long consultationId, long userId)
    {
        var member = await _db.Queryable<RcConsultationMemberEntity>()
            .Where(x => x.ConsultationId == consultationId && x.UserId == userId)
            .FirstAsync();

        return NormalizeParticipantRole(member?.RoleCode);
    }

    private async Task<string> ResolveHostIdAsync(RcConsultationEntity consultation)
    {
        var hostMember = await _db.Queryable<RcConsultationMemberEntity>()
            .Where(x => x.ConsultationId == consultation.Id && x.RoleCode == "HOST")
            .FirstAsync();

        if (hostMember != null)
        {
            return hostMember.UserId.ToString();
        }

        return consultation.ApplyDoctorId > 0
            ? consultation.ApplyDoctorId.ToString()
            : CurrentUserId.ToString();
    }

    private async Task<string> EnsureMeetingRoomAsync(RcConsultationEntity consultation)
    {
        if (!string.IsNullOrWhiteSpace(consultation.MeetingRoomNo))
        {
            return consultation.MeetingRoomNo;
        }

        consultation.MeetingRoomNo = $"rc-{consultation.Id}";
        await _db.Updateable(consultation)
            .UpdateColumns(x => new { x.MeetingRoomNo })
            .ExecuteCommandAsync();

        return consultation.MeetingRoomNo;
    }

    private RtcTokenOutput BuildTokenOutput(string roomId, string token, DateTime expiresAt, string role, string displayName, string hostId)
    {
        var expiresIn = (int)Math.Max(1, (expiresAt - DateTime.UtcNow).TotalSeconds);
        return new RtcTokenOutput
        {
            RoomId = roomId,
            Token = token,
            ExpiresIn = expiresIn,
            Role = role,
            DisplayName = displayName,
            UserId = CurrentUserId.ToString(),
            HostId = hostId,
            SignalingUrl = BuildSignalingUrl(roomId)
        };
    }

    private string BuildSignalingUrl(string roomId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.SignalingBaseUrl)
            ? _options.BaseUrl.Replace("http", "ws").TrimEnd('/') + "/ws/medical"
            : _options.SignalingBaseUrl;
        return $"{baseUrl.TrimEnd('/')}/{roomId}";
    }

    private static string NormalizeParticipantRole(string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return ParticipantRole.Expert;
        }

        var code = roleCode.Trim().ToUpperInvariant();
        return code switch
        {
            ParticipantRole.Host => ParticipantRole.Host,
            ParticipantRole.Observer => ParticipantRole.Observer,
            _ => ParticipantRole.Expert
        };
    }

    private static RtcRoomStatusOutput MapRoom(SfuRoomSnapshot room)
    {
        var participants = room.Participants ?? Array.Empty<SfuParticipantSnapshot>();
        return new RtcRoomStatusOutput
        {
            RoomId = room.RoomId,
            ConsultationId = room.ConsultationId,
            HostId = room.HostId,
            State = room.State,
            ParticipantCount = room.ParticipantCount,
            CreatedAt = room.CreatedAt,
            ClosedAt = room.ClosedAt,
            Participants = participants.Select(p => new RtcRoomParticipantOutput
            {
                ParticipantId = p.ParticipantId,
                DisplayName = p.DisplayName,
                Role = p.Role,
                JoinedAt = p.JoinedAt,
                Muted = p.Muted
            }).ToList()
        };
    }

    private record RtcTokenCacheItem(string Token, DateTime ExpiresAt, string Role, string DisplayName, string HostId);

    private static class ParticipantRole
    {
        public const string Host = "HOST";
        public const string Expert = "EXPERT";
        public const string Observer = "OBSERVER";
    }

    #endregion
}

