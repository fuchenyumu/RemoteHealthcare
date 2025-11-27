namespace PurestAdmin.Application.RemoteHealthcare.Sfu;

public interface ISfuApiClient
{
    Task<SfuTokenResponse> RequestTokenAsync(SfuTokenRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SfuRoomSnapshot>> GetRoomsAsync(CancellationToken cancellationToken = default);

    Task<SfuRoomSnapshot?> GetRoomAsync(string roomId, CancellationToken cancellationToken = default);

    Task<SfuServiceStatusResponse> GetServiceStatusAsync(CancellationToken cancellationToken = default);
}


