using PurestAdmin.Application.RcConsultationTimelineServices.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace PurestAdmin.Application.RcConsultationTimelineServices;
/// <summary>
/// RcConsultationTimeline服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
public class RcConsultationTimelineService(ISqlSugarClient db) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<PagedList<RcConsultationTimelineOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var query = _db.Queryable<RcConsultationTimelineEntity, UserEntity>((timeline, user) => new object[]
        {
            JoinType.Left, timeline.CreateBy == user.Id
        })
        .Where((timeline, user) => timeline.ConsultationId == input.ConsultationId);

        if (!string.IsNullOrWhiteSpace(input.EventCode))
        {
            query = query.Where((timeline, user) => timeline.EventCode == input.EventCode);
        }

        var pagedList = await query
            .OrderBy((timeline, user) => timeline.CreateTime)
            .Select((timeline, user) => new RcConsultationTimelineOutput
            {
                Id = timeline.Id,
                ConsultationId = timeline.ConsultationId,
                EventCode = timeline.EventCode,
                EventContent = timeline.EventContent,
                Snapshot = timeline.Snapshot.ToString(),
                CreateTime = timeline.CreateTime,
                CreateBy = timeline.CreateBy,
                CreateByName = user.Name
            })
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return pagedList;
    }

    /// <summary>
    /// 单条查询
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<RcConsultationTimelineOutput> GetAsync(long id)
    {
        return await _db.Queryable<RcConsultationTimelineEntity, UserEntity>((timeline, user) => new object[]
        {
            JoinType.Left, timeline.CreateBy == user.Id
        })
        .Where((timeline, user) => timeline.Id == id)
        .Select((timeline, user) => new RcConsultationTimelineOutput
        {
            Id = timeline.Id,
            ConsultationId = timeline.ConsultationId,
            EventCode = timeline.EventCode,
            EventContent = timeline.EventContent,
            Snapshot = timeline.Snapshot.ToString(),
            CreateTime = timeline.CreateTime,
            CreateBy = timeline.CreateBy,
            CreateByName = user.Name
        })
        .FirstAsync() ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
    }
}
