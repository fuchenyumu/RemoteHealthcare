using PurestAdmin.Application.RcConsultationAttachmentServices.Dtos;
using PurestAdmin.Application.RcConsultationMemberServices.Dtos;
using PurestAdmin.Application.RcConsultationReportServices.Dtos;
using PurestAdmin.Application.RcConsultationServices.Dtos;
using PurestAdmin.Application.RcConsultationTimelineServices.Dtos;
using PurestAdmin.Application.RcPatientCaseServices.Dtos;
using PurestAdmin.Application.RcPatientPackServices.Dtos;
using PurestAdmin.Multiplex.Contracts.IAdminUser;
using System.Text.Json;

namespace PurestAdmin.Application.RcConsultationServices;

/// <summary>
/// 远程会诊服务
/// </summary>
[ApiExplorerSettings(GroupName = ApiExplorerGroupConst.REMOTEHEALTHCARE)]
public class RcConsultationService(ISqlSugarClient db, ICurrentUser currentUser) : ApplicationService
{
    private readonly ISqlSugarClient _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    private long CurrentUserId => _currentUser?.Id ?? 0;

    /// <summary>
    /// 会诊分页列表
    /// </summary>
    public async Task<PagedList<RcConsultationSummaryOutput>> GetPagedListAsync(GetConsultationPagedListInput input)
    {
        var keyword = input.Keyword?.Trim();

        var query = _db.Queryable<RcConsultationEntity, RcPatientCaseEntity, RcPatientPackEntity, UserEntity, OrganizationEntity, OrganizationEntity, UserEntity, UserEntity>(
            (consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => new object[]
            {
                JoinType.Left, consultation.CaseId == patientCase.Id,
                JoinType.Left, consultation.PackId == pack.Id,
                JoinType.Left, consultation.ApplyDoctorId == applyDoctor.Id,
                JoinType.Left, consultation.ApplyOrgId == applyOrg.Id,
                JoinType.Left, consultation.TargetOrgId == targetOrg.Id,
                JoinType.Left, consultation.TargetExpertId == targetExpert.Id,
                JoinType.Left, consultation.AuditDoctorId == auditDoctor.Id
            });

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert) =>
                patientCase.PatientName.Contains(keyword) ||
                consultation.Purpose.Contains(keyword) ||
                pack.PackNo.Contains(keyword) ||
                consultation.TargetDepartment.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(input.ConsultationStatus))
        {
            query = query.Where((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => consultation.ConsultationStatus == input.ConsultationStatus);
        }

        if (!string.IsNullOrWhiteSpace(input.EmergencyLevel))
        {
            query = query.Where((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => consultation.EmergencyLevel == input.EmergencyLevel);
        }

        if (input.ApplyDoctorId.HasValue)
        {
            query = query.Where((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => consultation.ApplyDoctorId == input.ApplyDoctorId.Value);
        }

        if (input.TargetOrgId.HasValue)
        {
            query = query.Where((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => consultation.TargetOrgId == input.TargetOrgId.Value);
        }

        if (input.CreateTimeStart.HasValue)
        {
            query = query.Where((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => consultation.CreateTime >= input.CreateTimeStart.Value);
        }

        if (input.CreateTimeEnd.HasValue)
        {
            var end = input.CreateTimeEnd.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => consultation.CreateTime <= end);
        }

        var pagedList = await query
            .OrderBy((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => consultation.CreateTime, OrderByType.Desc)
            .Select((consultation, patientCase, pack, applyDoctor, applyOrg, targetOrg, targetExpert, auditDoctor) => new RcConsultationSummaryOutput
            {
                Id = consultation.Id,
                CaseId = consultation.CaseId,
                PackId = consultation.PackId,
                PackNo = pack.PackNo,
                PatientName = patientCase.PatientName,
                PatientType = patientCase.PatientType,
                EmergencyLevel = consultation.EmergencyLevel,
                ConsultationStatus = consultation.ConsultationStatus,
                Purpose = consultation.Purpose,
                DesiredStartTime = consultation.DesiredStartTime,
                DesiredEndTime = consultation.DesiredEndTime,
                ScheduledStartTime = consultation.ScheduledStartTime,
                ScheduledEndTime = consultation.ScheduledEndTime,
                MeetingRoomNo = consultation.MeetingRoomNo,
                RtcChannelId = consultation.RtcChannelId,
                RtcVendor = consultation.RtcVendor,
                ApplyDoctorId = consultation.ApplyDoctorId,
                ApplyDoctorName = applyDoctor.Name,
                ApplyOrgId = consultation.ApplyOrgId,
                ApplyOrgName = applyOrg.Name,
                TargetOrgId = consultation.TargetOrgId,
                TargetOrgName = targetOrg.Name,
                TargetDepartment = consultation.TargetDepartment,
                TargetExpertId = consultation.TargetExpertId,
                TargetExpertName = targetExpert.Name,
                AuditDoctorId = consultation.AuditDoctorId,
                AuditDoctorName = auditDoctor.Name,
                AuditTime = consultation.AuditTime,
                AuditComment = consultation.AuditComment,
                CloseReason = consultation.CloseReason,
                CreateBy = consultation.CreateBy,
                CreateTime = consultation.CreateTime,
                UpdateBy = consultation.UpdateBy,
                UpdateTime = consultation.UpdateTime,
                Remark = consultation.Remark
            })
            .ToPurestPagedListAsync(input.PageIndex, input.PageSize);

        return pagedList;
    }

    /// <summary>
    /// 会诊详情
    /// </summary>
    public async Task<RcConsultationDetailOutput> GetAsync(long id)
    {
        var consultation = await _db.Queryable<RcConsultationEntity>().InSingleAsync(id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        var detail = consultation.Adapt<RcConsultationDetailOutput>();

        // 补充基础信息
        detail.PatientCase = await BuildPatientCaseAsync(consultation.CaseId);
        detail.PatientPack = consultation.PackId.HasValue ? await BuildPatientPackAsync(consultation.PackId.Value) : null;

        await FillUserAndOrganizationInfoAsync(detail, consultation);

        detail.Attachments = await BuildAttachmentOutputsAsync(id);
        detail.Members = await BuildMemberOutputsAsync(id);
        detail.Timeline = await BuildTimelineOutputsAsync(id);
        detail.Report = await BuildReportOutputAsync(id);

        return detail;
    }

    /// <summary>
    /// 创建会诊
    /// </summary>
    public async Task<long> AddAsync(AddRcConsultationInput input)
    {
        await EnsurePatientCaseExists(input.CaseId);

        RcConsultationEntity entity = input.Adapt<RcConsultationEntity>();
        entity.ConsultationStatus = RemoteHealthcareConsts.ConsultationStatus.PendingReview;
        entity.CreateBy = CurrentUserId;
        entity.CreateTime = DateTime.Now;

        var id = await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();

        await AppendTimelineAsync(id, RemoteHealthcareConsts.TimelineEvents.Submitted, "提交会诊申请");

        return id;
    }

    /// <summary>
    /// 编辑会诊基础信息
    /// </summary>
    public async Task PutAsync(long id, PutRcConsultationInput input)
    {
        var entity = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (!CanEdit(entity.ConsultationStatus))
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "当前状态不允许修改会诊信息");
        }

        input.Adapt(entity);
        entity.UpdateBy = CurrentUserId;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 审核会诊
    /// </summary>
    public async Task AuditAsync(long id, AuditRcConsultationInput input) 
    {
        var entity = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (entity.ConsultationStatus != RemoteHealthcareConsts.ConsultationStatus.PendingReview)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "仅待审核状态支持审批");
        }

        entity.AuditDoctorId = CurrentUserId;
        entity.AuditComment = input.Comment;
        entity.AuditTime = DateTime.Now;
        entity.UpdateBy = CurrentUserId;
        entity.UpdateTime = DateTime.Now;
        entity.ConsultationStatus = input.Approved
            ? RemoteHealthcareConsts.ConsultationStatus.WaitSchedule
            : RemoteHealthcareConsts.ConsultationStatus.Rejected;

        await _db.Updateable(entity).ExecuteCommandAsync();

        var eventCode = input.Approved ? RemoteHealthcareConsts.TimelineEvents.Approved : RemoteHealthcareConsts.TimelineEvents.Rejected;
        var content = input.Approved ? "审核通过" : "审核驳回";
        if (!string.IsNullOrWhiteSpace(input.Comment))
        {
            content += $"：{input.Comment}";
        }

        await AppendTimelineAsync(entity.Id, eventCode, content);
    }

    /// <summary>
    /// 排期会诊
    /// </summary>
    public async Task ScheduleAsync(long id, ScheduleRcConsultationInput input)
    {
        var entity = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (entity.ConsultationStatus != RemoteHealthcareConsts.ConsultationStatus.WaitSchedule &&
            entity.ConsultationStatus != RemoteHealthcareConsts.ConsultationStatus.Scheduled)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "当前状态不支持排期");
        }

        entity.ScheduledStartTime = input.ScheduledStartTime;
        entity.ScheduledEndTime = input.ScheduledEndTime;
        entity.MeetingRoomNo = input.MeetingRoomNo;
        entity.ConsultationStatus = RemoteHealthcareConsts.ConsultationStatus.Scheduled;
        entity.UpdateBy = CurrentUserId;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();

        await AppendTimelineAsync(entity.Id, RemoteHealthcareConsts.TimelineEvents.Scheduled,
            $"排期完成：{input.ScheduledStartTime:yyyy-MM-dd HH:mm} ~ {input.ScheduledEndTime:yyyy-MM-dd HH:mm}");
    }

    /// <summary>
    /// 更新RTC信息
    /// </summary>
    public async Task UpdateRtcAsync(UpdateConsultationRtcInput input)
    {
        var entity = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == input.ConsultationId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        entity.MeetingRoomNo = input.MeetingRoomNo;
        entity.RtcChannelId = input.RtcChannelId;
        entity.RtcVendor = input.RtcVendor;
        entity.UpdateBy = CurrentUserId;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();

        await AppendTimelineAsync(entity.Id, RemoteHealthcareConsts.TimelineEvents.Scheduled,
            "更新音视频配置", new { input.MeetingRoomNo, input.RtcChannelId, input.RtcVendor });
    }

    /// <summary>
    /// 开始会诊
    /// </summary>
    public async Task StartAsync(long id, StartRcConsultationInput input)
    {
        var entity = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (entity.ConsultationStatus != RemoteHealthcareConsts.ConsultationStatus.Scheduled)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "仅待会诊状态支持开始");
        }

        entity.ConsultationStatus = RemoteHealthcareConsts.ConsultationStatus.InProgress;
        entity.UpdateBy = CurrentUserId;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();

        await AppendTimelineAsync(entity.Id, RemoteHealthcareConsts.TimelineEvents.Started, "会诊已开始");
    }

    /// <summary>
    /// 完成会诊
    /// </summary>
    public async Task FinishAsync(long id, FinishRcConsultationInput input)
    {
        var entity = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (entity.ConsultationStatus != RemoteHealthcareConsts.ConsultationStatus.InProgress)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "仅进行中的会诊支持结束");
        }

        entity.ConsultationStatus = RemoteHealthcareConsts.ConsultationStatus.Finished;
        entity.UpdateBy = CurrentUserId;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();

        await AppendTimelineAsync(entity.Id, RemoteHealthcareConsts.TimelineEvents.Ended,
            string.IsNullOrWhiteSpace(input.Summary) ? "会诊已结束" : input.Summary);
    }

    /// <summary>
    /// 关闭会诊
    /// </summary>
    public async Task CloseAsync(long id, CloseRcConsultationInput input)
    {
        var entity = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (entity.ConsultationStatus == RemoteHealthcareConsts.ConsultationStatus.Closed)
        {
            return;
        }

        entity.ConsultationStatus = RemoteHealthcareConsts.ConsultationStatus.Closed;
        entity.CloseReason = input.CloseReason;
        entity.UpdateBy = CurrentUserId;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();

        await AppendTimelineAsync(entity.Id, RemoteHealthcareConsts.TimelineEvents.Closed, input.CloseReason);
    }

    /// <summary>
    /// 删除会诊（仅草稿/驳回）
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        var entity = await _db.Queryable<RcConsultationEntity>().FirstAsync(x => x.Id == id)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult);

        if (entity.ConsultationStatus is not (RemoteHealthcareConsts.ConsultationStatus.Draft or RemoteHealthcareConsts.ConsultationStatus.Rejected))
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.IllegalOperation, "仅草稿或驳回的会诊可以删除");
        }

        await _db.Deleteable<RcConsultationEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
    }

    /// <summary>
    /// 统计概览
    /// </summary>
    public async Task<RcConsultationOverviewOutput> GetOverviewAsync()
    {
        var total = await _db.Queryable<RcConsultationEntity>().CountAsync();

        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);
        var today = await _db.Queryable<RcConsultationEntity>()
            .Where(x => x.CreateTime >= todayStart && x.CreateTime <= todayEnd)
            .CountAsync();

        var pendingReview = await _db.Queryable<RcConsultationEntity>()
            .Where(x => x.ConsultationStatus == RemoteHealthcareConsts.ConsultationStatus.PendingReview)
            .CountAsync();

        var finished = await _db.Queryable<RcConsultationEntity>()
            .Where(x => x.ConsultationStatus == RemoteHealthcareConsts.ConsultationStatus.Finished)
            .CountAsync();

        return new RcConsultationOverviewOutput
        {
            Total = total,
            Today = today,
            PendingReview = pendingReview,
            Finished = finished
        };
    }

    /// <summary>
    /// 趋势统计（按天）
    /// </summary>
    public async Task<List<RcConsultationTrendItemOutput>> GetTrendAsync(int days = 30)
    {
        if (days <= 0)
        {
            days = 30;
        }

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

    #region private helpers

    private async Task<RcPatientCaseDetailOutput> BuildPatientCaseAsync(long caseId)
    {
        var entity = await _db.Queryable<RcPatientCaseEntity>().FirstAsync(x => x.Id == caseId)
            ?? throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "患者快照不存在");

        var output = entity.Adapt<RcPatientCaseDetailOutput>();
        output.GestationalWeekValue = entity.GestationalWeeks;
        return output;
    }

    private async Task<RcPatientPackOutput> BuildPatientPackAsync(long packId)
    {
        var entity = await _db.Queryable<RcPatientPackEntity>().FirstAsync(x => x.Id == packId);
        return entity?.Adapt<RcPatientPackOutput>();
    }

    private async Task FillUserAndOrganizationInfoAsync(RcConsultationSummaryOutput summary, RcConsultationEntity entity)
    {
        var userIds = new HashSet<long> { entity.ApplyDoctorId };
        if (entity.TargetExpertId.HasValue)
        {
            userIds.Add(entity.TargetExpertId.Value);
        }
        if (entity.AuditDoctorId.HasValue)
        {
            userIds.Add(entity.AuditDoctorId.Value);
        }

        var users = await _db.Queryable<UserEntity>().Where(u => userIds.Contains(u.Id)).ToListAsync();

        summary.ApplyDoctorName = users.FirstOrDefault(u => u.Id == entity.ApplyDoctorId)?.Name;
        summary.TargetExpertName = entity.TargetExpertId.HasValue
            ? users.FirstOrDefault(u => u.Id == entity.TargetExpertId)?.Name
            : null;
        summary.AuditDoctorName = entity.AuditDoctorId.HasValue
            ? users.FirstOrDefault(u => u.Id == entity.AuditDoctorId)?.Name
            : null;

        var orgIds = new HashSet<long> { entity.ApplyOrgId };
        if (entity.TargetOrgId.HasValue)
        {
            orgIds.Add(entity.TargetOrgId.Value);
        }

        var organizations = await _db.Queryable<OrganizationEntity>().Where(o => orgIds.Contains(o.Id)).ToListAsync();
        summary.ApplyOrgName = organizations.FirstOrDefault(o => o.Id == entity.ApplyOrgId)?.Name;
        summary.TargetOrgName = entity.TargetOrgId.HasValue
            ? organizations.FirstOrDefault(o => o.Id == entity.TargetOrgId)?.Name
            : null;
    }

    private async Task<IReadOnlyList<RcConsultationAttachmentOutput>> BuildAttachmentOutputsAsync(long consultationId)
    {
        var list = await _db.Queryable<RcConsultationAttachmentEntity, FileRecordEntity>((attachment, file) => new object[]
            {
                JoinType.Left, attachment.FileId == file.Id
            })
            .Where((attachment, file) => attachment.ConsultationId == consultationId)
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
            .ToListAsync();

        return list;
    }

    private async Task<IReadOnlyList<RcConsultationMemberOutput>> BuildMemberOutputsAsync(long consultationId)
    {
        var list = await _db.Queryable<RcConsultationMemberEntity, UserEntity, OrganizationEntity>((member, user, org) => new object[]
            {
                JoinType.Inner, member.UserId == user.Id,
                JoinType.Left, member.OrgId == org.Id
            })
            .Where((member, user, org) => member.ConsultationId == consultationId)
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
            .ToListAsync();

        return list;
    }

    private async Task<IReadOnlyList<RcConsultationTimelineOutput>> BuildTimelineOutputsAsync(long consultationId)
    {
        var list = await _db.Queryable<RcConsultationTimelineEntity>()
            .Where(x => x.ConsultationId == consultationId)
            .OrderBy(x => x.CreateTime)
            .Select(x => new RcConsultationTimelineOutput
            {
                Id = x.Id,
                ConsultationId = x.ConsultationId,
                EventCode = x.EventCode,
                EventContent = x.EventContent,
                Snapshot = x.Snapshot.ToString(),
                CreateTime = x.CreateTime,
                CreateBy = x.CreateBy
            })
            .ToListAsync();

        var creatorIds = list.Select(x => x.CreateBy).Where(x => x > 0).Distinct().ToArray();
        if (creatorIds.Length > 0)
        {
            var users = await _db.Queryable<UserEntity>().Where(u => creatorIds.Contains(u.Id)).ToListAsync();
            foreach (var item in list)
            {
                item.CreateByName = users.FirstOrDefault(u => u.Id == item.CreateBy)?.Name;
            }
        }

        return list;
    }

    private async Task<RcConsultationReportDetailOutput> BuildReportOutputAsync(long consultationId)
    {
        var entity = await _db.Queryable<RcConsultationReportEntity>().FirstAsync(x => x.ConsultationId == consultationId);
        if (entity == null)
        {
            return null;
        }

        var output = entity.Adapt<RcConsultationReportDetailOutput>();
        if (entity.SignerId.HasValue)
        {
            var user = await _db.Queryable<UserEntity>().FirstAsync(x => x.Id == entity.SignerId.Value);
            output.SignerName = user?.Name;
        }
        return output;
    }

    private async Task EnsurePatientCaseExists(long caseId)
    {
        var exists = await _db.Queryable<RcPatientCaseEntity>().AnyAsync(x => x.Id == caseId);
        if (!exists)
        {
            throw PersistdValidateException.Message(ErrorTipsEnum.NoResult, "患者快照不存在");
        }
    }

    private bool CanEdit(string status)
    {
        return status is RemoteHealthcareConsts.ConsultationStatus.Draft
            or RemoteHealthcareConsts.ConsultationStatus.PendingReview
            or RemoteHealthcareConsts.ConsultationStatus.WaitSchedule
            or RemoteHealthcareConsts.ConsultationStatus.Rejected;
    }

    private async Task AppendTimelineAsync(long consultationId, string eventCode, string content, object snapshot = null)
    {
        var timeline = new RcConsultationTimelineEntity
        {
            ConsultationId = consultationId,
            EventCode = eventCode,
            EventContent = content,
            Snapshot = snapshot == null ? null : JsonSerializer.Serialize(snapshot),
            CreateBy = CurrentUserId,
                CreateTime = DateTime.Now
        };

        await _db.Insertable(timeline).ExecuteReturnSnowflakeIdAsync();
    }

    #endregion
}
