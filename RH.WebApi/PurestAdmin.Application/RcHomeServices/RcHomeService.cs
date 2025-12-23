using PurestAdmin.Application.RcHomeServices.Dtos;
using PurestAdmin.Application.RcConsultationServices.Dtos;
using PurestAdmin.Multiplex.Contracts.Consts;

namespace PurestAdmin.Application.RcHomeServices;

/// <summary>
/// 远程会诊-首页工作台
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
public class RcHomeService(ISqlSugarClient db) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;

    /// <summary>
    /// 首页工作台数据（概览 + 趋势/分布 + 待办/动态）
    /// </summary>
    /// <param name="days">趋势天数（默认15）</param>
    public async Task<RcHomeDashboardOutput> GetDashboardAsync(int days = 15)
    {
        if (days <= 0 || days > 90)
        {
            days = 15;
        }

        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);

        // 注意：ISqlSugarClient 在本项目中注册为 SqlSugarScope 单例，
        // Npgsql 同一连接不支持并发命令，因此这里避免 Task.WhenAll 并发执行，改为串行 await。
        // 概览指标
        var totalConsultations = await _db.Queryable<RcConsultationEntity>().CountAsync();
        var todayConsultations = await _db.Queryable<RcConsultationEntity>()
            .Where(x => x.CreateTime >= todayStart && x.CreateTime <= todayEnd)
            .CountAsync();
        var pendingReview = await _db.Queryable<RcConsultationEntity>()
            .Where(x => x.ConsultationStatus == RemoteHealthcareConsts.ConsultationStatus.PendingReview)
            .CountAsync();
        var finished = await _db.Queryable<RcConsultationEntity>()
            .Where(x => x.ConsultationStatus == RemoteHealthcareConsts.ConsultationStatus.Finished)
            .CountAsync();

        var pendingReports = await _db.Queryable<RcConsultationReportEntity>()
            .Where(x => x.ReportStatus == RemoteHealthcareConsts.ReportStatus.Draft
                     || x.ReportStatus == RemoteHealthcareConsts.ReportStatus.Signing)
            .CountAsync();

        var failedSyncTaskCount = await _db.Queryable<RcSyncTaskEntity>()
            .Where(x => x.TaskStatus == RemoteHealthcareConsts.SyncTaskStatus.Failed)
            .CountAsync();

        // 待办：未来7天已排期会诊
        var upcomingStart = DateTime.Now.AddMinutes(-30);
        var upcomingEnd = DateTime.Now.Date.AddDays(7).AddTicks(-1);
        var upcomingConsultations = await _db
            .Queryable<RcConsultationEntity, RcPatientCaseEntity, OrganizationEntity, OrganizationEntity, UserEntity>(
                (c, pc, applyOrg, targetOrg, expert) => new object[]
                {
                    JoinType.Left, c.CaseId == pc.Id,
                    JoinType.Left, c.ApplyOrgId == applyOrg.Id,
                    JoinType.Left, c.TargetOrgId == targetOrg.Id,
                    JoinType.Left, c.TargetExpertId == expert.Id
                })
            .Where((c, pc, applyOrg, targetOrg, expert) =>
                c.ScheduledStartTime.HasValue &&
                c.ScheduledStartTime.Value >= upcomingStart &&
                c.ScheduledStartTime.Value <= upcomingEnd)
            .Where((c, pc, applyOrg, targetOrg, expert) =>
                c.ConsultationStatus == RemoteHealthcareConsts.ConsultationStatus.Scheduled ||
                c.ConsultationStatus == RemoteHealthcareConsts.ConsultationStatus.InProgress)
            .OrderBy((c, pc, applyOrg, targetOrg, expert) => c.ScheduledStartTime, OrderByType.Asc)
            .Take(8)
            .Select((c, pc, applyOrg, targetOrg, expert) => new RcHomeUpcomingConsultationItemOutput
            {
                Id = c.Id,
                PatientName = pc.PatientName,
                EmergencyLevel = c.EmergencyLevel,
                ConsultationStatus = c.ConsultationStatus,
                ScheduledStartTime = c.ScheduledStartTime,
                ScheduledEndTime = c.ScheduledEndTime,
                ApplyOrgName = applyOrg.Name,
                TargetOrgName = targetOrg.Name,
                TargetExpertName = expert.Name
            })
            .ToListAsync();

        // 动态：最近10条时间线
        var recentTimelines = await _db
            .Queryable<RcConsultationTimelineEntity, UserEntity>((t, u) => new object[]
            {
                JoinType.Left, t.CreateBy == u.Id
            })
            .OrderBy((t, u) => t.CreateTime, OrderByType.Desc)
            .Take(10)
            .Select((t, u) => new RcHomeTimelineItemOutput
            {
                Id = t.Id,
                ConsultationId = t.ConsultationId,
                EventCode = t.EventCode,
                EventContent = t.EventContent,
                CreateTime = t.CreateTime,
                CreateByName = u.Name
            })
            .ToListAsync();

        // 趋势（按天）
        var trend = await BuildTrendAsync(days);

        // 紧急程度分布
        var emergencyDistribution = await _db.Queryable<RcConsultationEntity>()
            .GroupBy(x => x.EmergencyLevel)
            .Select(x => new RcConsultationDistributionOutput
            {
                Name = x.EmergencyLevel,
                Value = SqlFunc.AggregateCount(x.Id)
            })
            .ToListAsync();

        // 专家排行（复用 RcConsultationService 逻辑：这里按已完成会诊数统计）
        var expertRanking = await _db.Queryable<RcConsultationEntity, UserEntity>((c, u) => c.TargetExpertId == u.Id)
            .Where((c, u) => c.ConsultationStatus == RemoteHealthcareConsts.ConsultationStatus.Finished)
            .GroupBy((c, u) => u.Name)
            .Select((c, u) => new RcExpertRankingOutput
            {
                Name = u.Name,
                Count = SqlFunc.AggregateCount(c.Id),
                AvgResponseHours = 2.5 // 演示用：后续可替换为真实计算（从提交到首次响应）
            })
            // 多表查询 + Select 投影后，排序别名需一致：使用 MergeTable 将结果集转成单表再排序
            .MergeTable()
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        return new RcHomeDashboardOutput
        {
            Overview = new RcHomeOverviewOutput
            {
                TotalConsultations = totalConsultations,
                TodayConsultations = todayConsultations,
                PendingReviewConsultations = pendingReview,
                FinishedConsultations = finished,
                PendingReports = pendingReports,
                FailedSyncTasks = failedSyncTaskCount
            },
            Trend = trend,
            EmergencyDistribution = emergencyDistribution,
            ExpertRanking = expertRanking,
            UpcomingConsultations = upcomingConsultations,
            RecentTimelines = recentTimelines
        };
    }

    private async Task<List<RcConsultationTrendItemOutput>> BuildTrendAsync(int days)
    {
        var points = new List<RcConsultationTrendItemOutput>(days);
        for (int i = days - 1; i >= 0; i--)
        {
            var day = DateTime.Today.AddDays(-i);
            var next = day.AddDays(1);
            var count = await _db.Queryable<RcConsultationEntity>()
                .Where(x => x.CreateTime >= day && x.CreateTime < next)
                .CountAsync();

            points.Add(new RcConsultationTrendItemOutput
            {
                Date = day.ToString("yyyy-MM-dd"),
                Count = count
            });
        }
        return points;
    }
}

