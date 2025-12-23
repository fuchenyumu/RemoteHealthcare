using PurestAdmin.Application.RcConsultationServices.Dtos;

namespace PurestAdmin.Application.RcHomeServices.Dtos;

/// <summary>
/// 首页-概览指标
/// </summary>
public class RcHomeOverviewOutput
{
    public int TotalConsultations { get; set; }
    public int TodayConsultations { get; set; }
    public int PendingReviewConsultations { get; set; }
    public int FinishedConsultations { get; set; }

    public int PendingReports { get; set; }
    public int FailedSyncTasks { get; set; }
}

/// <summary>
/// 首页-会诊日程项（近几天/今日待办）
/// </summary>
public class RcHomeUpcomingConsultationItemOutput
{
    public long Id { get; set; }
    public string PatientName { get; set; }
    public string EmergencyLevel { get; set; }
    public string ConsultationStatus { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public string ApplyOrgName { get; set; }
    public string TargetOrgName { get; set; }
    public string TargetExpertName { get; set; }
}

/// <summary>
/// 首页-最近动态（时间线）
/// </summary>
public class RcHomeTimelineItemOutput
{
    public long Id { get; set; }
    public long ConsultationId { get; set; }
    public string EventCode { get; set; }
    public string EventContent { get; set; }
    public DateTime CreateTime { get; set; }
    public string CreateByName { get; set; }
}

/// <summary>
/// 首页-仪表盘数据
/// </summary>
public class RcHomeDashboardOutput
{
    public RcHomeOverviewOutput Overview { get; set; }

    public List<RcConsultationTrendItemOutput> Trend { get; set; }
    public List<RcConsultationDistributionOutput> EmergencyDistribution { get; set; }
    public List<RcExpertRankingOutput> ExpertRanking { get; set; }

    public List<RcHomeUpcomingConsultationItemOutput> UpcomingConsultations { get; set; }
    public List<RcHomeTimelineItemOutput> RecentTimelines { get; set; }
}

