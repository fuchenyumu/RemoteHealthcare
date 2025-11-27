using PurestAdmin.Application.RcConsultationAttachmentServices.Dtos;
using PurestAdmin.Multiplex.Contracts.IAdminUser;
using Microsoft.AspNetCore.Mvc;

namespace PurestAdmin.Application.RcConsultationAttachmentServices;

/// <summary>
/// 会诊附件服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
[Route("api/v1/rc-consultation-attachment")]
public class RcConsultationAttachmentService(ISqlSugarClient db, ICurrentUser currentUser) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 会诊附件分页
    /// </summary>
    [HttpGet]
    public async Task<PagedList<RcConsultationAttachmentOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var query = _db.Queryable<RcConsultationAttachmentEntity, FileRecordEntity>((attachment, file) => new object[]
        {
            JoinType.Left, attachment.FileId == file.Id
        })
        .Where((attachment, file) => attachment.ConsultationId == input.ConsultationId);

        if (!string.IsNullOrWhiteSpace(input.AttachmentType))
        {
            query = query.Where((attachment, file) => attachment.AttachmentType == input.AttachmentType);
        }

        var paged = await query
            .OrderBy((attachment, file) => attachment.CreateTime, OrderByType.Desc)
            .Select((attachment, file) => new RcConsultationAttachmentOutput
            {
                Id = attachment.Id,
                ConsultationId = attachment.ConsultationId,
                FileId = attachment.FileId,
                FileName = file.FileName,
                ExternalUri = attachment.ExternalUri,
                AttachmentType = attachment.AttachmentType,
                SourceSystem = attachment.SourceSystem,
                Description = attachment.Description,
                CreateTime = attachment.CreateTime
            })
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return paged;
    }

    /// <summary>
    /// 获取附件详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<RcConsultationAttachmentOutput> GetAsync(long id)
    {
        return await _db.Queryable<RcConsultationAttachmentEntity, FileRecordEntity>((attachment, file) => new object[]
            {
                JoinType.Left, attachment.FileId == file.Id
            })
            .Where((attachment, file) => attachment.Id == id)
            .Select((attachment, file) => new RcConsultationAttachmentOutput
            {
                Id = attachment.Id,
                ConsultationId = attachment.ConsultationId,
                FileId = attachment.FileId,
                FileName = file.FileName,
                ExternalUri = attachment.ExternalUri,
                AttachmentType = attachment.AttachmentType,
                SourceSystem = attachment.SourceSystem,
                Description = attachment.Description,
                CreateTime = attachment.CreateTime
            })
            .FirstAsync() ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
    }

    /// <summary>
    /// 新增附件
    /// </summary>
    [HttpPost]
    public async Task<long> AddAsync(AddRcConsultationAttachmentInput input)
    {
        await EnsureConsultationExists(input.ConsultationId);

        if (!input.FileId.HasValue && string.IsNullOrWhiteSpace(input.ExternalUri))
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.InvalidParams, "附件需指定文件或外部链接");
        }

        var entity = input.Adapt<RcConsultationAttachmentEntity>();
        entity.CreateBy = _currentUser?.Id ?? 0;
        entity.CreateTime = DateTime.Now;

        return await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
    }

    /// <summary>
    /// 更新附件描述信息
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task PutAsync(long id, PutRcConsultationAttachmentInput input)
    {
        var entity = await _db.Queryable<RcConsultationAttachmentEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        input.Adapt(entity);
        entity.UpdateBy = _currentUser?.Id ?? 0;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除附件
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task DeleteAsync(long id)
    {
        var exists = await _db.Queryable<RcConsultationAttachmentEntity>().AnyAsync(x => x.Id == id);
        if (!exists)
        {
            return;
        }

        await _db.Deleteable<RcConsultationAttachmentEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
    }

    private async Task EnsureConsultationExists(long consultationId)
    {
        var exists = await _db.Queryable<RcConsultationEntity>().AnyAsync(x => x.Id == consultationId);
        if (!exists)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "会诊记录不存在");
        }
    }
}

