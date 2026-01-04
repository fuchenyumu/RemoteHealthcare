namespace PurestAdmin.Application.RcConsultationShareServices.Dtos;

/// <summary>
/// 分享Token验证输出
/// </summary>
public class ValidateShareTokenOutput
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 会诊ID
    /// </summary>
    public long? ConsultationId { get; set; }

    /// <summary>
    /// 会诊信息
    /// </summary>
    public ConsultationInfoOutput? Consultation { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 访客默认显示名称
    /// </summary>
    public string? VisitorDisplayName { get; set; }
}

/// <summary>
/// 会诊简要信息
/// </summary>
public class ConsultationInfoOutput
{
    /// <summary>
    /// 会诊ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    public string PatientName { get; set; }

    /// <summary>
    /// 会诊目的
    /// </summary>
    public string Purpose { get; set; }

    /// <summary>
    /// 会诊状态
    /// </summary>
    public string ConsultationStatus { get; set; }

    /// <summary>
    /// 排期开始时间
    /// </summary>
    public DateTime? ScheduledStartTime { get; set; }

    /// <summary>
    /// 会议房间号
    /// </summary>
    public string? MeetingRoomNo { get; set; }
}
