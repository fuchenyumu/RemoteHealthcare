using Mapster;
using Microsoft.AspNetCore.Mvc;
using PurestAdmin.Application.RcPatientCaseServices.Dtos;
using PurestAdmin.Multiplex.Contracts.IAdminUser;

namespace PurestAdmin.Application.RcPatientCaseServices;
/// <summary>
/// RcPatientCase服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
[Route("api/v1/rc-patient-case")]
public class RcPatientCaseService(ISqlSugarClient db, ICurrentUser currentUser) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("paged-list")]
    public async Task<PagedList<RcPatientCaseSummaryOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var keyword = input.Keyword?.Trim();

        var query = _db.Queryable<RcPatientCaseEntity>();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.PatientName.Contains(keyword) || x.Phone.Contains(keyword) || x.IdCard.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(input.PatientType))
        {
            query = query.Where(x => x.PatientType == input.PatientType);
        }

        if (!string.IsNullOrWhiteSpace(input.DataSource))
        {
            query = query.Where(x => x.DataSource == input.DataSource);
        }

        var pagedList = await query
            .OrderBy(x => x.CreateTime, OrderByType.Desc)
            .Select(x => new RcPatientCaseSummaryOutput
            {
                Id = x.Id,
                PatientName = x.PatientName,
                PatientSex = x.PatientSex,
                PatientType = x.PatientType,
                Phone = x.Phone,
                GestationalWeeks = x.GestationalWeeks,
                ChildAgeMonths = x.ChildAgeMonths,
                DataSource = x.DataSource,
                CreateTime = x.CreateTime
            })
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return pagedList;
    }

    /// <summary>
    /// 单条查询
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:long}")]
    public async Task<RcPatientCaseDetailOutput> GetAsync(long id)
    {
        var entity = await _db.Queryable<RcPatientCaseEntity>().InSingleAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var output = entity.Adapt<RcPatientCaseDetailOutput>();
        output.GestationalWeekValue = entity.GestationalWeeks;
        return output;
    }

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<long> AddAsync(AddRcPatientCaseInput input)
    {
        var entity = input.Adapt<RcPatientCaseEntity>();
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
    public async Task PutAsync(long id, PutRcPatientCaseInput input)
    {
        var entity = await _db.Queryable<RcPatientCaseEntity>().InSingleAsync(id) ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
        var newEntity = input.Adapt(entity);
        newEntity.UpdateBy = _currentUser?.Id ?? 0;
        newEntity.UpdateTime = DateTime.Now;
        _ = await _db.Updateable(newEntity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:long}")]
    public async Task DeleteAsync(long id)
    {
        var entity = await _db.Queryable<RcPatientCaseEntity>().InSingleAsync(id) ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var referenced = await _db.Queryable<RcConsultationEntity>().AnyAsync(x => x.CaseId == id);
        if (referenced)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "已有会诊引用该患者，无法删除");
        }

        _ = await _db.Deleteable(entity).ExecuteCommandAsync();
    }
}
