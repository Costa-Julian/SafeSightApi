using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Models.Dtos.Responses;

public class AdminDashboardResponse
{
    public DateTime GeneratedAt { get; set; }
    public StatsOverviewResponse Stats { get; set; } = new();
    public List<AlertResponse> UnresolvedAlerts { get; set; } = new();
    public List<CitizenReport> RecentReports { get; set; } = new();
    public List<AdminActivityItem> RecentActivity { get; set; } = new();
    public List<HeatmapCellDto> Heatmap { get; set; } = new();
}
