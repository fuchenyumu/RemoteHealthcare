using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 更新会诊音视频信息
/// </summary>
public class UpdateConsultationRtcInput
{
    [Required]
    public long ConsultationId { get; set; }

    [MaxLength(50)]
    public string MeetingRoomNo { get; set; }

    [MaxLength(64)]
    public string RtcChannelId { get; set; }

    [MaxLength(30)]
    public string RtcVendor { get; set; }
}

