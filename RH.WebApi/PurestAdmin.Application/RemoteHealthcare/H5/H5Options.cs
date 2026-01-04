namespace PurestAdmin.Application.RemoteHealthcare.H5;

/// <summary>
/// H5 配置选项
/// </summary>
public class H5Options
{
    /// <summary>
    /// H5 评价基础 URL
    /// </summary>
    public string EvaluationBaseUrl { get; set; }

    /// <summary>
    /// H5 评价路径
    /// </summary>
    public string EvaluationPath { get; set; }

    /// <summary>
    /// 获取完整的评价 URL
    /// </summary>
    /// <param name="consultationId">会诊 ID</param>
    /// <returns>完整的评价 URL</returns>
    public string GetEvaluationUrl(long consultationId)
    {
        var baseUrl = EvaluationBaseUrl.TrimEnd('/');
        var path = EvaluationPath.TrimStart('/');
        return $"{baseUrl}/{path}/{consultationId}";
    }
}
