using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PurestAdmin.Application.RcPatientPackServices.Dtos;
using PurestAdmin.Core.File;
using PurestAdmin.Core.File.Containers;
using PurestAdmin.Multiplex.Contracts.Consts;
using PurestAdmin.Multiplex.Contracts.IAdminUser;
using PurestAdmin.SqlSugar.Entity;
using PurestAdmin.SqlSugar.RcEntity;
using SqlSugar;
using Yitter.IdGenerator;

namespace PurestAdmin.Application.RcPatientPackServices;
/// <summary>
/// RcPatientPack服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
[Route("api/v1/rc-patient-pack")]
public class RcPatientPackService(ISqlSugarClient db, ICurrentUser currentUser, IFileCommand<RcPatientPackContainer> fileCommand) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IFileCommand<RcPatientPackContainer> _fileCommand = fileCommand;

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("paged-list")]
    public async Task<PagedList<RcPatientPackOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var query = _db.Queryable<RcPatientPackEntity>();

        if (input.CaseId.HasValue)
        {
            query = query.Where(x => x.CaseId == input.CaseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.PackStatus))
        {
            query = query.Where(x => x.PackStatus == input.PackStatus);
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x => x.PackNo.Contains(keyword));
        }

        var pagedEntity = await query
            .OrderBy(x => x.CreateTime, OrderByType.Desc)
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return pagedEntity.Adapt<PagedList<RcPatientPackOutput>>();
    }

    /// <summary>
    /// 单条查询
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:long}")]
    public async Task<RcPatientPackOutput> GetAsync(long id)
    {
        var entity = await EnsurePackExists(id);
        return entity.Adapt<RcPatientPackOutput>();
    }

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<long> AddAsync(AddRcPatientPackInput input)
    {
        await EnsurePatientCaseExists(input.CaseId);

        var entity = new RcPatientPackEntity
        {
            CaseId = input.CaseId,
            PackNo = GeneratePackNo(),
            PackStatus = string.IsNullOrWhiteSpace(input.PackStatus) ? RemoteHealthcareConsts.PackStatus.Pending : input.PackStatus,
            ExpireTime = input.ExpireTime,
            FileCount = 0,
            TotalSize = 0,
            SyncTaskId = input.SyncTaskId,
            Remark = input.Remark,
            CreateBy = _currentUser?.Id ?? 0,
            CreateTime = DateTime.Now
        };

        return await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
    }

    /// <summary>
    /// 编辑
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("{id:long}")]
    public async Task PutAsync(long id, PutRcPatientPackInput input)
    {
        var entity = await EnsurePackExists(id);

        if (input.CaseId != entity.CaseId)
        {
            await EnsurePatientCaseExists(input.CaseId);
            entity.CaseId = input.CaseId;
        }

        entity.PackStatus = string.IsNullOrWhiteSpace(input.PackStatus) ? entity.PackStatus : input.PackStatus;
        entity.ExpireTime = input.ExpireTime;
        entity.SyncTaskId = input.SyncTaskId;
        entity.Remark = input.Remark;
        entity.UpdateBy = _currentUser?.Id ?? 0;
        entity.UpdateTime = DateTime.Now;

        _ = await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:long}")]
    public async Task DeleteAsync(long id)
    {
        var entity = await EnsurePackExists(id);

        if (entity.PackStatus is RemoteHealthcareConsts.PackStatus.Completed)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "已完成的打包记录不可删除");
        }
        var attachments = await _db.Queryable<RcPatientPackFileEntity>().Where(x => x.PackId == id).ToListAsync();
        foreach (var attachment in attachments)
        {
            await _fileCommand.RemoveAsync(attachment.FileId);
            await _db.Deleteable<FileRecordEntity>().Where(x => x.Id == attachment.FileId).ExecuteCommandAsync();
        }
        if (attachments.Count > 0)
        {
            await _db.Deleteable<RcPatientPackFileEntity>().Where(x => x.PackId == id).ExecuteCommandAsync();
        }
        _ = await _db.Deleteable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取资料包附件
    /// </summary>
    [HttpGet("{id:long}/files")]
    public async Task<List<RcPatientPackFileOutput>> GetFilesAsync(long id)
    {
        _ = await EnsurePackExists(id);
        var list = await _db.Queryable<RcPatientPackFileEntity, FileRecordEntity>((file, record) => new object[]
            {
                JoinType.Inner, file.FileId == record.Id
            })
            .Where((file, record) => file.PackId == id)
            .OrderBy((file, record) => file.CreateTime, OrderByType.Desc)
            .Select((file, record) => new RcPatientPackFileOutput
            {
                Id = file.Id,
                PackId = file.PackId,
                FileId = file.FileId,
                FileName = file.FileName ?? record.FileName,
                FileSize = file.FileSize > 0 ? file.FileSize : record.FileSize * 1024L,
                CreateTime = file.CreateTime
            })
            .ToListAsync();

        return list;
    }

    /// <summary>
    /// 上传资料包附件
    /// </summary>
    [HttpPost("{id:long}/files")]
    public async Task<RcPatientPackFileOutput> UploadFileAsync(long id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.InvalidParams, "请选择需要上传的文件");
        }

        _ = await EnsurePackExists(id);

        long fileId = 0;
        long attachmentId = 0;
        try
        {
            fileId = await _fileCommand.SaveAsync(file);

            var attachment = new RcPatientPackFileEntity
            {
                PackId = id,
                FileId = fileId,
                FileName = file.FileName,
                FileSize = file.Length,
                CreateBy = _currentUser?.Id ?? 0,
                CreateTime = DateTime.Now
            };

            attachmentId = await _db.Insertable(attachment).ExecuteReturnSnowflakeIdAsync();
            attachment.Id = attachmentId;
            await UpdatePackFileMetrics(id);

            return new RcPatientPackFileOutput
            {
                Id = attachment.Id,
                PackId = id,
                FileId = fileId,
                FileName = attachment.FileName,
                FileSize = attachment.FileSize,
                CreateTime = attachment.CreateTime
            };
        }
        catch
        {
            if (attachmentId > 0)
            {
                await _db.Deleteable<RcPatientPackFileEntity>().Where(x => x.Id == attachmentId).ExecuteCommandAsync();
            }
            if (fileId > 0)
            {
                await _fileCommand.RemoveAsync(fileId);
                await _db.Deleteable<FileRecordEntity>().Where(x => x.Id == fileId).ExecuteCommandAsync();
            }
            throw;
        }
    }

    /// <summary>
    /// 删除资料包附件
    /// </summary>
    [HttpDelete("{id:long}/files/{fileId:long}")]
    public async Task DeleteFileAsync(long id, long fileId)
    {
        var attachment = await _db.Queryable<RcPatientPackFileEntity>().FirstAsync(x => x.Id == fileId && x.PackId == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        await _fileCommand.RemoveAsync(attachment.FileId);
        await _db.Deleteable<RcPatientPackFileEntity>().Where(x => x.Id == fileId).ExecuteCommandAsync();
        await _db.Deleteable<FileRecordEntity>().Where(x => x.Id == attachment.FileId).ExecuteCommandAsync();
        await UpdatePackFileMetrics(id);
    }

    private async Task EnsurePatientCaseExists(long caseId)
    {
        var exists = await _db.Queryable<RcPatientCaseEntity>().AnyAsync(x => x.Id == caseId);
        if (!exists)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "患者快照不存在");
        }
    }

    private async Task<RcPatientPackEntity> EnsurePackExists(long packId)
    {
        return await _db.Queryable<RcPatientPackEntity>().InSingleAsync(packId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
    }

    private async Task UpdatePackFileMetrics(long packId)
    {
        var stats = await _db.Queryable<RcPatientPackFileEntity>()
            .Where(x => x.PackId == packId)
            .GroupBy(x => x.PackId)
            .Select(x => new
            {
                Count = SqlFunc.AggregateCount(x.Id),
                Size = SqlFunc.AggregateSum(x.FileSize)
            })
            .FirstAsync();

        var fileCount = stats?.Count ?? 0;
        var totalSize = stats?.Size ?? 0;

        await _db.Updateable<RcPatientPackEntity>()
            .Where(x => x.Id == packId)
            .SetColumns(x => x.FileCount == fileCount)
            .SetColumns(x => x.TotalSize == totalSize)
            .ExecuteCommandAsync();
    }

    private static string GeneratePackNo()
    {
        return $"PK{YitIdHelper.NextId()}";
    }
    
}
