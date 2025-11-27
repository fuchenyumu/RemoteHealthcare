using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 会诊排期请求
/// </summary>
public class ScheduleRcConsultationInput
{
    [Required]
    public DateTime ScheduledStartTime { get; set; }

    [Required]
    public DateTime ScheduledEndTime { get; set; }

    [MaxLength(50)]
    public string MeetingRoomNo { get; set; }
}

