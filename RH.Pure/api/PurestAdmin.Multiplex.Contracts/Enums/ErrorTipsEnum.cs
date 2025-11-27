// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using System.ComponentModel;

namespace PurestAdmin.Multiplex.Contracts.Enums;
/// <summary>
/// 业务日志错误码
/// </summary>
public enum ErrorTipsEnum
{
    /// <summary>
    /// 记录不存在
    /// </summary>
    [Description("记录不存在")]
    NoResult,
    /// <summary>
    /// 无此权限
    /// </summary>
    [Description("无此权限")]
    NoPermission,
    /// <summary>
    /// 非法操作
    /// </summary>
    [Description("非法操作")]
    IllegalOperation,
    /// <summary>
    /// 参数错误
    /// </summary>
    [Description("参数错误")]
    InvalidParams,
}
