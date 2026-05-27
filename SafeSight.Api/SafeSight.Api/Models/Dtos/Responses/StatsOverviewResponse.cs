namespace SafeSight.Api.Models.Dtos.Responses;

public class StatsOverviewResponse
{
    public int TotalAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int CancelledAlerts { get; set; }
    public long TotalReports { get; set; }
    public long TotalAwarenessReports { get; set; }
    public long TotalInfoReports { get; set; }
}
