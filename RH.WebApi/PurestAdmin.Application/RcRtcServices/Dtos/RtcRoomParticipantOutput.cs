namespace PurestAdmin.Application.RcRtcServices.Dtos;

public class RtcRoomParticipantOutput
{
    public string ParticipantId { get; set; }

    public string DisplayName { get; set; }

    public string Role { get; set; }

    public DateTime JoinedAt { get; set; }

    public bool Muted { get; set; }
}

