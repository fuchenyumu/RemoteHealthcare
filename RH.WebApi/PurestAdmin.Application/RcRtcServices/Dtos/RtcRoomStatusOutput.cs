namespace PurestAdmin.Application.RcRtcServices.Dtos;

public class RtcRoomStatusOutput
{
    public string RoomId { get; set; }

    public long ConsultationId { get; set; }

    public string HostId { get; set; }

    public string State { get; set; }

    public int ParticipantCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public IReadOnlyList<RtcRoomParticipantOutput> Participants { get; set; } = Array.Empty<RtcRoomParticipantOutput>();
}

