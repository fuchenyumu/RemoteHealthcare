namespace PurestAdmin.Application.RcRtcServices.Dtos;

public class RtcServiceStatusOutput
{
    public string Status { get; set; }

    public string App { get; set; }

    public int RoomCount { get; set; }

    public int ParticipantCount { get; set; }

    public DateTime StartedAt { get; set; }

    public long UptimeSeconds { get; set; }
}

