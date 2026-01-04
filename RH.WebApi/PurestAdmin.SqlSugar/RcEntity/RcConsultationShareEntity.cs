using SqlSugar;

namespace PurestAdmin.SqlSugar.RcEntity;

/// <summary>
/// 会诊分享链接表
/// </summary>
[SugarTable("PUREST_RC_CONSULTATION_SHARE")]
public class RcConsultationShareEntity : BaseEntity
{
    /// <summary>
    /// 会诊ID
    /// </summary>
    [SugarColumn(ColumnName = "consultation_id")]
    public long ConsultationId { get; set; }

    /// <summary>
    /// 分享码(唯一标识)
    /// </summary>
    [SugarColumn(ColumnName = "share_token", Length = 128)]
    public string ShareToken { get; set; }

    /// <summary>
    /// 创建人用户ID
    /// </summary>
    [SugarColumn(ColumnName = "creator_user_id")]
    public long CreatorUserId { get; set; }

    /// <summary>
    /// 访客显示名称(可选,用于匿名访问时自定义显示名称)
    /// </summary>
    [SugarColumn(ColumnName = "visitor_display_name", Length = 100, IsNullable = true)]
    public string? VisitorDisplayName { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnName = "expire_time")]
    public DateTime ExpireTime { get; set; }

    /// <summary>
    /// 最大使用次数(0表示不限制)
    /// </summary>
    [SugarColumn(ColumnName = "max_usage_count")]
    public int MaxUsageCount { get; set; }

    /// <summary>
    /// 已使用次数
    /// </summary>
    [SugarColumn(ColumnName = "used_count")]
    public int UsedCount { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 首次访问时间
    /// </summary>
    [SugarColumn(ColumnName = "first_access_time", IsNullable = true)]
    public DateTime? FirstAccessTime { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    [SugarColumn(ColumnName = "last_access_time", IsNullable = true)]
    public DateTime? LastAccessTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true)]
    public string? Remark { get; set; }
}
