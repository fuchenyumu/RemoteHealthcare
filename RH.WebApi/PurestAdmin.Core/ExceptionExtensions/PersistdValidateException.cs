// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using System.ComponentModel;

using Microsoft.OpenApi.Extensions;

using Volo.Abp;

namespace PurestAdmin.Core.ExceptionExtensions;
public static class PersistdValidateException
{
    /// <summary>
    /// 抛出持久化异常信息
    /// </summary>
    /// <param name="errorMessage">异常消息</param>
    /// <returns>异常实例</returns>
    public static BusinessException Message(string errorMessage)
    {
        return new UserFriendlyException(errorMessage, "Qa.001");
    }

    public static BusinessException Message(string errorMessage, string errorMessageDetail)
    {
        return new UserFriendlyException(errorMessage, "Qa.001", errorMessageDetail);
    }
    /// <summary>
    /// 抛出持久化异常信息
    /// </summary>
    /// <param name="errorMessage">异常消息</param>
    /// <returns>异常实例</returns>
    public static BusinessException Message(Enum tipsEnum)
    {
        var description = tipsEnum.GetAttributeOfType<DescriptionAttribute>() ?? throw Message("未找到对应异常的枚举描述");
        return new UserFriendlyException(description.Description, "Qa.001");
    }
    /// <summary>
    /// 抛出持久化异常信息
    /// </summary>
    /// <param name="errorMessage">异常消息</param>
    /// <returns>异常实例</returns>
    public static BusinessException Message(Enum tipsEnum, string errorMessageDetail)
    {
        var description = tipsEnum.GetAttributeOfType<DescriptionAttribute>() ?? throw Message("未找到对应异常的枚举描述");
        return new UserFriendlyException(description.Description, "Qa.001", errorMessageDetail);
    }
}
