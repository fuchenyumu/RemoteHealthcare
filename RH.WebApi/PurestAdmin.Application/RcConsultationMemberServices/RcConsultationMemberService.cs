using PurestAdmin.Application.RcConsultationMemberServices.Dtos;
using PurestAdmin.Multiplex.Contracts.IAdminUser;
using Microsoft.AspNetCore.Mvc;

namespace PurestAdmin.Application.RcConsultationMemberServices;

/// <summary>
/// 会诊成员服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
[Route("api/v1/rc-consultation-member")]
public class RcConsultationMemberService(ISqlSugarClient db, ICurrentUser currentUser) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 成员分页
    /// </summary>
    [HttpGet]
    public async Task<PagedList<RcConsultationMemberOutput>> GetPagedListAsync(GetPagedListInput input)
    {
        var query = _db.Queryable<RcConsultationMemberEntity, UserEntity, OrganizationEntity>((member, user, org) => new object[]
        {
            JoinType.Inner, member.UserId == user.Id,
            JoinType.Left, member.OrgId == org.Id
        })
        .Where((member, user, org) => member.ConsultationId == input.ConsultationId);

        if (!string.IsNullOrWhiteSpace(input.RoleCode))
        {
            query = query.Where((member, user, org) => member.RoleCode == input.RoleCode);
        }

        if (!string.IsNullOrWhiteSpace(input.JoinStatus))
        {
            query = query.Where((member, user, org) => member.JoinStatus == input.JoinStatus);
        }

        var paged = await query
            .OrderBy((member, user, org) => member.CreateTime, OrderByType.Desc)
            .Select((member, user, org) => new RcConsultationMemberOutput
            {
                Id = member.Id,
                ConsultationId = member.ConsultationId,
                UserId = member.UserId,
                UserName = user.Name,
                OrgId = member.OrgId,
                OrgName = org.Name,
                RoleCode = member.RoleCode,
                JoinStatus = member.JoinStatus,
                JoinTime = member.JoinTime,
                LeaveTime = member.LeaveTime,
                Remark = member.Remark
            })
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return paged;
    }

    /// <summary>
    /// 成员详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<RcConsultationMemberOutput> GetAsync(long id)
    {
        return await _db.Queryable<RcConsultationMemberEntity, UserEntity, OrganizationEntity>((member, user, org) => new object[]
        {
            JoinType.Inner, member.UserId == user.Id,
            JoinType.Left, member.OrgId == org.Id
        })
        .Where((member, user, org) => member.Id == id)
        .Select((member, user, org) => new RcConsultationMemberOutput
        {
            Id = member.Id,
            ConsultationId = member.ConsultationId,
            UserId = member.UserId,
            UserName = user.Name,
            OrgId = member.OrgId,
            OrgName = org.Name,
            RoleCode = member.RoleCode,
            JoinStatus = member.JoinStatus,
            JoinTime = member.JoinTime,
            LeaveTime = member.LeaveTime,
            Remark = member.Remark
        })
        .FirstAsync() ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);
    }

    /// <summary>
    /// 添加成员
    /// </summary>
    [HttpPost]
    public async Task<long> AddAsync(AddRcConsultationMemberInput input)
    {
        await EnsureConsultationExists(input.ConsultationId);

        var exists = await _db.Queryable<RcConsultationMemberEntity>().AnyAsync(x => x.ConsultationId == input.ConsultationId && x.UserId == input.UserId && x.JoinStatus != RemoteHealthcareConsts.MemberStatus.Exited);
        if (exists)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.InvalidParams, "该成员已在会诊中");
        }

        var entity = input.Adapt<RcConsultationMemberEntity>();
        entity.CreateBy = _currentUser?.Id ?? 0;
        entity.CreateTime = DateTime.Now;

        return await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
    }

    /// <summary>
    /// 更新成员信息
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task PutAsync(long id, PutRcConsultationMemberInput input)
    {
        var entity = await _db.Queryable<RcConsultationMemberEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        input.Adapt(entity);
        entity.UpdateBy = _currentUser?.Id ?? 0;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除成员
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task DeleteAsync(long id)
    {
        var entity = await _db.Queryable<RcConsultationMemberEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

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
}

