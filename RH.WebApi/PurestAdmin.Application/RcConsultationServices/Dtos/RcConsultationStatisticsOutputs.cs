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