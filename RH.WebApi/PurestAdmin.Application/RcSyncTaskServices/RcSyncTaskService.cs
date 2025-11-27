using PurestAdmin.Application.RcSyncTaskServices.Dtos;
using PurestAdmin.Multiplex.Contracts.IAdminUser;
using System.Text.Json;

namespace PurestAdmin.Application.RcSyncTaskServices;
/// <summary>
/// RcSyncTask服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
public class RcSyncTaskService(ISqlSugarClient db, ICurrentUser currentUser) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<PagedList<RcSyncTaskOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var query = _db.Queryable<RcSyncTaskEntity>();

        if (!string.IsNullOrWhiteSpace(input.TaskType))
        {
            query = query.Where(x => x.TaskType == input.TaskType);
        }

        if (!string.IsNullOrWhiteSpace(input.TaskStatus))
        {
            query = query.Where(x => x.TaskStatus == input.TaskStatus);
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x => x.TaskNo.Contains(keyword));
        }

        var pagedEntity = await query
            .OrderBy(x => x.CreateTime, OrderByType.Desc)
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return pagedEntity.Adapt<PagedList<RcSyncTaskOutput>>();
    }

    /// <summary>
    /// 单条查询
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:long}")]
    public async Task<RcSyncTaskOutput> GetAsync(long id)
    {
        var entity = await _db.Queryable<RcSyncTaskEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        return entity.Adapt<RcSyncTaskOutput>();
    }

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<long> AddAsync(AddRcSyncTaskInput input)
    {
        var entity = input.Adapt<RcSyncTaskEntity>();
        entity.RequestPayload = input.RequestPayload == null ? null : JsonSerializer.Serialize(input.RequestPayload);
        entity.ResponsePayload = input.ResponsePayload == null ? null : JsonSerializer.Serialize(input.ResponsePayload);
        entity.CreateBy = _currentUser?.Id ?? 0;
        entity.CreateTime = DateTime.Now;
        return await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
    }

    /// <summary>
    /// 编辑
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("{id:long}")]
    public async Task PutAsync(long id, PutRcSyncTaskInput input)
    {
        var entity = await _db.Queryable<RcSyncTaskEntity>().FirstAsync(x => x.Id == id) ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        input.Adapt(entity);
        entity.RequestPayload = input.RequestPayload == null ? null : JsonSerializer.Serialize(input.RequestPayload);
        entity.ResponsePayload = input.ResponsePayload == null ? null : JsonSerializer.Serialize(input.ResponsePayload);
        entity.CompletedTime = input.CompletedTime;
        entity.UpdateBy = _currentUser?.Id ?? 0;
        entity.UpdateTime = DateTime.Now;
        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:long}")]
    public async Task DeleteAsync(long id)
    {
        var entity = await _db.Queryable<RcSyncTaskEntity>().InSingleAsync(id) ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        if (entity.TaskStatus == RemoteHealthcareConsts.SyncTaskStatus.Success)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "已完成的任务不可删除");
        }

        await _db.Deleteable(entity).ExecuteCommandAsync();
    }
}
