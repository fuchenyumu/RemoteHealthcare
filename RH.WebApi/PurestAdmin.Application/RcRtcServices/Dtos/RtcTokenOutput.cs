namespace PurestAdmin.Application.RcRtcServices.Dtos;

public class RtcTokenOutput
{
    public string Token { get; set; }

    public int ExpiresIn { get; set; }

    public string RoomId { get; set; }

    public string Role { get; set; }

    public string DisplayName { get; set; }

    public string UserId { get; set; }

    public string HostId { get; set; }

    public string SignalingUrl { get; set; }
}

