using PurestAdmin.Application.RcConsultationReportServices.Dtos;
using PurestAdmin.Multiplex.Contracts.IAdminUser;
using Microsoft.AspNetCore.Mvc;

namespace PurestAdmin.Application.RcConsultationReportServices;

/// <summary>
/// 会诊结论报告服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
[Route("api/v1/rc-consultation-report")]
public class RcConsultationReportService(ISqlSugarClient db, ICurrentUser currentUser) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 报告列表（按会诊ID）
    /// </summary>
    [HttpGet]
    public async Task<PagedList<RcConsultationReportDetailOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var query = _db.Queryable<RcConsultationReportEntity>().Where(x => x.ConsultationId == input.ConsultationId);

        if (!string.IsNullOrWhiteSpace(input.ReportStatus))
        {
            query = query.Where(x => x.ReportStatus == input.ReportStatus);
        }

        var paged = await query
            .OrderBy(x => x.CreateTime, OrderByType.Desc)
            .Select(x => x.Adapt<RcConsultationReportDetailOutput>())
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return paged;
    }

    /// <summary>
    /// 获取报告详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<RcConsultationReportDetailOutput> GetAsync(long id)
    {
        var entity = await _db.Queryable<RcConsultationReportEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var output = entity.Adapt<RcConsultationReportDetailOutput>();

        if (entity.SignerId.HasValue)
        {
            var signer = await _db.Queryable<UserEntity>().FirstAsync(x => x.Id == entity.SignerId.Value);
            output.SignerName = signer?.Name;
        }

        return output;
    }

    /// <summary>
    /// 保存报告内容
    /// </summary>
    [HttpPost]
    public async Task<long> SaveAsync(AddRcConsultationReportInput input)
    {
        await EnsureConsultationExists(input.ConsultationId);

        var entity = await _db.Queryable<RcConsultationReportEntity>().FirstAsync(x => x.ConsultationId == input.ConsultationId);
        if (entity == null)
        {
            entity = input.Adapt<RcConsultationReportEntity>();
            entity.ReportStatus ??= RemoteHealthcareConsts.ReportStatus.Draft;
            entity.CreateBy = _currentUser?.Id ?? 0;
            entity.CreateTime = DateTime.Now;
            return await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
        }

        if (entity.ReportStatus == RemoteHealthcareConsts.ReportStatus.Archived)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "报告已归档，无法修改");
        }

        input.Adapt(entity);
        entity.UpdateBy = _currentUser?.Id ?? 0;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();
        return entity.Id;
    }

    /// <summary>
    /// 更新报告内容
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task PutAsync(long id, PutRcConsultationReportInput input)
    {
        var entity = await _db.Queryable<RcConsultationReportEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (entity.ReportStatus == RemoteHealthcareConsts.ReportStatus.Archived)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "报告已归档，无法修改");
        }

        input.Adapt(entity);
        entity.UpdateBy = _currentUser?.Id ?? 0;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 签署报告
    /// </summary>
    [HttpPost("sign")]
    public async Task SignAsync(SignRcConsultationReportInput input)
    {
        var entity = await _db.Queryable<RcConsultationReportEntity>().FirstAsync(x => x.ConsultationId == input.ConsultationId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "请先保存会诊报告内容");

        entity.SignerId = input.SignerId;
        entity.SignedTime = DateTime.Now;
        entity.SignatureFileId = input.SignatureFileId;
        entity.ReportStatus = RemoteHealthcareConsts.ReportStatus.Signed;
        entity.UpdateBy = _currentUser?.Id ?? 0;
        entity.UpdateTime = DateTime.Now;
        entity.Remark = input.Remark;

        await _db.Updateable(entity).ExecuteCommandAsync();

        await AppendTimelineAsync(input.ConsultationId, RemoteHealthcareConsts.TimelineEvents.ReportSigned, "会诊报告已签署");
    }

    /// <summary>
    /// 删除报告（仅草稿阶段）
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task DeleteAsync(long id)
    {
        var entity = await _db.Queryable<RcConsultationReportEntity>().InSingleAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (entity.ReportStatus == RemoteHealthcareConsts.ReportStatus.Signed || entity.ReportStatus == RemoteHealthcareConsts.ReportStatus.Archived)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "已签署/归档的报告不允许删除");
        }

        await _db.Deleteable(entity).ExecuteCommandAsync();
    }

    private async Task EnsureConsultationExists(long consultationId)
    {
        var exists = await _db.Queryable<RcConsultationEntity>().AnyAsync(x => x.Id == consultationId);
        if (!exists)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "会诊记录不存在");
        }
    }

    private async Task AppendTimelineAsync(long consultationId, string eventCode, string content)
    {
        var timeline = new RcConsultationTimelineEntity
        {
            ConsultationId = consultationId,
            EventCode = eventCode,
            EventContent = content,
            CreateBy = _currentUser?.Id ?? 0,
            CreateTime = DateTime.Now
        };

        await _db.Insertable(timeline).ExecuteReturnSnowflakeIdAsync();
    }
}