using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

namespace PurestAdmin.Application.RemoteHealthcare.Sfu;

public sealed class SfuApiClient : ISfuApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<SfuOptions> _options;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public SfuApiClient(HttpClient httpClient, IOptions<SfuOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<SfuTokenResponse> RequestTokenAsync(SfuTokenRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(CombinePath(_options.Value.TokenEndpoint), request, _serializerOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<SfuTokenResponse>(_serializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SFU 返回了空的 Token 响应");
        return payload;
    }

    public async Task<IReadOnlyList<SfuRoomSnapshot>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(CombinePath(_options.Value.RoomsEndpoint), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<List<SfuRoomSnapshot>>(_serializerOptions, cancellationToken)
            ?? [];
        return payload;
    }

    public async Task<SfuRoomSnapshot?> GetRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{CombinePath(_options.Value.RoomsEndpoint).TrimEnd('/')}/{roomId}";
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<SfuRoomSnapshot>(_serializerOptions, cancellationToken);
    }

    public async Task<SfuServiceStatusResponse> GetServiceStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(CombinePath(_options.Value.StatusEndpoint), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<SfuServiceStatusResponse>(_serializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SFU 返回了空的状态数据");
        return payload;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"调用 SFU 接口失败（{(int)response.StatusCode}）：{detail}");
    }

    private static string CombinePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "/";
        }

        return relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
    }
}

public sealed record SfuTokenRequest(
    [property: JsonPropertyName("roomId")] string RoomId,
    [property: JsonPropertyName("consultationId")] long ConsultationId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("hostId")] string HostId
);

public sealed record SfuTokenResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("expiresIn")] int ExpiresIn
);

public sealed record SfuParticipantSnapshot(
    [property: JsonPropertyName("participantId")] string ParticipantId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("joinedAt")] DateTime JoinedAt,
    [property: JsonPropertyName("muted")] bool Muted
);

public sealed record SfuRoomSnapshot(
    [property: JsonPropertyName("roomId")] string RoomId,
    [property: JsonPropertyName("consultationId")] long ConsultationId,
    [property: JsonPropertyName("hostId")] string HostId,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("participantCount")] int ParticipantCount,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("closedAt")] DateTime? ClosedAt,
    [property: JsonPropertyName("participants")] IReadOnlyList<SfuParticipantSnapshot> Participants
);

public sealed record SfuServiceStatusResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("app")] string App,
    [property: JsonPropertyName("roomCount")] int RoomCount,
    [property: JsonPropertyName("participantCount")] int ParticipantCount,
    [property: JsonPropertyName("startedAt")] DateTime StartedAt,
    [property: JsonPropertyName("uptimeSeconds")] long UptimeSeconds
);


