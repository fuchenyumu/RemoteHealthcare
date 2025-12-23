
using System.ComponentModel.DataAnnotations;
using PurestAdmin.Multiplex.Contracts;

namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 会诊分页查询参数
/// </summary>
public class GetConsultationPagedListInput : PaginationParams
{
    /// <summary>
    /// 关键字（会诊目的、目标科室、患者名称等）
    /// </summary>
    [MaxLength(100)]
    public string Keyword { get; set; }

    /// <summary>
    /// 会诊状态
    /// </summary>
    [MaxLength(30)]
    public string ConsultationStatus { get; set; }

    /// <summary>
    /// 紧急程度
    /// </summary>
    [MaxLength(30)]
    public string EmergencyLevel { get; set; }

    /// <summary>
    /// 申请医生ID
    /// </summary>
    public long? ApplyDoctorId { get; set; }

    /// <summary>
    /// 目标医院ID
    /// </summary>
    public long? TargetOrgId { get; set; }

    /// <summary>
    /// 创建时间-起
    /// </summary>
    public DateTime? CreateTimeStart { get; set; }

    /// <summary>
    /// 创建时间-止
    /// </summary>
    public DateTime? CreateTimeEnd { get; set; }

    /// <summary>
    /// 排期开始时间-起（用于排期中心按月查询）
    /// </summary>
    public DateTime? ScheduledStartTimeStart { get; set; }

    /// <summary>
    /// 排期开始时间-止（用于排期中心按月查询）
    /// </summary>
    public DateTime? ScheduledStartTimeEnd { get; set; }
}
