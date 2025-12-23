namespace PurestAdmin.Application.RcConsultationServices.Dtos;

/// <summary>
/// 会诊统计概览
/// </summary>
public class RcConsultationOverviewOutput
{
    public int Total { get; set; }
    public int Today { get; set; }
    public int PendingReview { get; set; }
    public int Finished { get; set; }
}

/// <summary>
/// 会诊趋势项（按天）
/// </summary>
public class RcConsultationTrendItemOutput
{
    public string Date { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// 统计分布项
/// </summary>
public class RcConsultationDistributionOutput
{
    public string Name { get; set; }
    public int Value { get; set; }
}

/// <summary>
/// 专家排行项
/// </summary>
public class RcExpertRankingOutput
{
    public string Name { get; set; }
    public int Count { get; set; }
    public double AvgResponseHours { get; set; }
}
