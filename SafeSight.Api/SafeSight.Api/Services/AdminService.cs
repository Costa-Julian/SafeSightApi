using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Services;

public class AdminService : IAdminService
{
    private readonly IAlertsRepository _alertsRepository;
    private readonly IReportsRepository _reportsRepository;
    private readonly IHeatmapRepository _heatmapRepository;
    private readonly IStatsService _statsService;
    private readonly IAdminActivityRepository _activityRepository;

    public AdminService(
        IAlertsRepository alertsRepository,
        IReportsRepository reportsRepository,
        IHeatmapRepository heatmapRepository,
        IStatsService statsService,
        IAdminActivityRepository activityRepository)
    {
        _alertsRepository = alertsRepository;
        _reportsRepository = reportsRepository;
        _heatmapRepository = heatmapRepository;
        _statsService = statsService;
        _activityRepository = activityRepository;
    }

    public async Task<AdminDashboardResponse> GetDashboardAsync()
    {
        Task<StatsOverviewResponse> statsTask = _statsService.GetOverviewAsync();
        Task<List<Alert>> allAlertsTask = _alertsRepository.GetAllAsync();
        Task<List<CitizenReport>> reportsTask = _reportsRepository.GetReportsSinceAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        Task<List<AdminActivity>> activityTask = _activityRepository.GetRecentAsync(12);
        Task<List<HeatmapCell>> heatmapTask = _heatmapRepository.GetByAlertIdAsync(null);

        await Task.WhenAll(statsTask, allAlertsTask, reportsTask, activityTask, heatmapTask);

        List<AlertResponse> unresolvedAlerts = allAlertsTask.Result
            .Where(a => a.Status == AlertStatus.Active)
            .Select(AlertResponse.FromDomain)
            .ToList();

        List<CitizenReport> recentReports = reportsTask.Result
            .OrderByDescending(r => r.ReportedAt)
            .Take(12)
            .ToList();

        return new AdminDashboardResponse
        {
            GeneratedAt = DateTime.UtcNow,
            Stats = statsTask.Result,
            UnresolvedAlerts = unresolvedAlerts,
            RecentReports = recentReports,
            RecentActivity = MapActivity(activityTask.Result),
            Heatmap = heatmapTask.Result.Select(HeatmapCellDto.FromDomain).ToList()
        };
    }

    public async Task<List<AdminActivityItem>> GetActivityAsync(int limit)
    {
        List<AdminActivity> items = await _activityRepository.GetRecentAsync(limit);
        return MapActivity(items);
    }

    public async Task<List<AlertResponse>> GetAllAlertsAsync()
    {
        List<Alert> alerts = await _alertsRepository.GetAllAsync();
        return alerts.Select(AlertResponse.FromDomain).ToList();
    }

    public async Task<List<AdminReportRow>> GetReportsAsync(int limit)
    {
        Task<List<CitizenReport>> reportsTask = _reportsRepository.GetReportsSinceAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        Task<List<Alert>> alertsTask = _alertsRepository.GetAllAsync();

        await Task.WhenAll(reportsTask, alertsTask);

        Dictionary<string, Alert> alertsById = alertsTask.Result.ToDictionary(a => a.Id);

        return reportsTask.Result
            .OrderByDescending(r => r.ReportedAt)
            .Take(limit)
            .Select(r => BuildReportRow(r, alertsById))
            .ToList();
    }

    private static AdminReportRow BuildReportRow(CitizenReport report, Dictionary<string, Alert> alertsById)
    {
        alertsById.TryGetValue(report.AlertId, out Alert? alert);

        double? minutesToResolution = null;
        if (alert?.ResolvedAt != null)
            minutesToResolution = Math.Round((alert.ResolvedAt.Value - report.ReportedAt).TotalMinutes, 1);

        return new AdminReportRow
        {
            ReportId = report.Id.ToString(),
            AlertId = report.AlertId,
            AlertName = alert != null ? $"{alert.MissingPerson.FirstName} {alert.MissingPerson.LastName}" : report.AlertId,
            AlertAge = alert?.MissingPerson.Age ?? 0,
            EmitterId = alert?.EmitterId ?? 0,
            AlertStatus = alert?.Status.ToString() ?? "Active",
            ResolvedAt = alert?.ResolvedAt,
            ReportType = report.Type == ReportType.Info ? "Info" : "Awareness",
            Latitude = report.Latitude,
            Longitude = report.Longitude,
            ReportedAt = report.ReportedAt,
            Description = report.Description,
            PhotoUrl = report.PhotoUrl,
            MinutesToResolution = minutesToResolution
        };
    }

    private static List<AdminActivityItem> MapActivity(List<AdminActivity> items) =>
        items.Select(a => new AdminActivityItem
        {
            Id = a.Id,
            Kind = a.Kind,
            Title = a.Title,
            Description = a.Description,
            Timestamp = a.Timestamp,
            AlertId = a.AlertId
        }).ToList();
}
