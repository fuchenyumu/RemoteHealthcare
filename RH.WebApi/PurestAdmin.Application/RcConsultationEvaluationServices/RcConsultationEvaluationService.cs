using PurestAdmin.Application.RcConsultationEvaluationServices.Dtos;
using Microsoft.AspNetCore.Mvc;
using PurestAdmin.SqlSugar.RcEntity;
using PurestAdmin.SqlSugar;
using SqlSugar;

namespace PurestAdmin.Application.RcConsultationEvaluationServices;

/// <summary>
/// 远程会诊评价服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
[Route("api/v1/rc-consultation-evaluation")]
public class RcConsultationEvaluationService(ISqlSugarClient db) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;

    /// <summary>
    /// 分页查询评价
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<PagedList<RcEvaluationOutput>> GetPagedListAsync([FromQuery] GetRcEvaluationPagedListInput input)
    {
        var query = _db.Queryable<RcConsultationEvaluationEntity>();

        if (input.ConsultationId.HasValue)
        {
            query = query.Where(x => x.ConsultationId == input.ConsultationId.Value);
        }

        var pagedList = await query
            .OrderByDescending(x => x.CreateTime)
            .Select(x => new RcEvaluationOutput
            {
                Id = x.Id,
                ConsultationId = x.ConsultationId,
                PatientName = x.IsAnonymous ? "匿名" : x.PatientName,
                Score = x.Score,
                Tags = x.Tags,
                Content = x.Content,
                IsAnonymous = x.IsAnonymous,
                CreateTime = x.CreateTime
            })
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return pagedList;
    }

    /// <summary>
    /// 提交评价
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<long> CreateAsync(AddRcEvaluationInput input)
    {
        var entity = new RcConsultationEvaluationEntity
        {
            ConsultationId = input.ConsultationId,
            PatientName = input.PatientName,
            Score = input.Score,
            Tags = input.Tags,
            Content = input.Content,
            IsAnonymous = input.IsAnonymous
        };

        return await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
    }

    /// <summary>
    /// 获取单条评价
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:long}")]
    public async Task<RcEvaluationOutput> GetAsync(long id)
    {
        var x = await _db.Queryable<RcConsultationEvaluationEntity>()
            .Where(x => x.Id == id)
            .FirstAsync() ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        return new RcEvaluationOutput
        {
            Id = x.Id,
            ConsultationId = x.ConsultationId,
            PatientName = x.IsAnonymous ? "匿名" : x.PatientName,
            Score = x.Score,
            Tags = x.Tags,
            Content = x.Content,
            IsAnonymous = x.IsAnonymous,
            CreateTime = x.CreateTime
        };
    }
}






