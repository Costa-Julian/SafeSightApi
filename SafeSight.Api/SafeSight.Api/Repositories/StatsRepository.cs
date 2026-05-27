using SafeSight.Api.Common;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Repositories;

public class StatsRepository : IStatsRepository
{
    private readonly IAlertsRepository _alertsRepository;
    private readonly IReportsRepository _reportsRepository;

    public StatsRepository(IAlertsRepository alertsRepository, IReportsRepository reportsRepository)
    {
        _alertsRepository = alertsRepository;
        _reportsRepository = reportsRepository;
    }

    public async Task<StatsOverviewResponse> GetOverviewAsync()
    {
        int totalAlerts = await _alertsRepository.CountTotalAsync();
        int activeAlerts = await _alertsRepository.CountByStatusAsync(AlertStatus.Active);
        int resolvedAlerts = await _alertsRepository.CountByStatusAsync(AlertStatus.Resolved);
        int cancelledAlerts = await _alertsRepository.CountByStatusAsync(AlertStatus.Cancelled);

        // Lee reportes del último día como aproximación rápida para el overview del MVP.
        // En producción: mantener contadores en MongoDB actualizados por el sync service.
        DateTime since = DateTime.UtcNow.AddHours(-24);
        List<CitizenReport> recentReports = await _reportsRepository.GetReportsSinceAsync(since, DateTime.UtcNow);

        return new StatsOverviewResponse
        {
            TotalAlerts = totalAlerts,
            ActiveAlerts = activeAlerts,
            ResolvedAlerts = resolvedAlerts,
            CancelledAlerts = cancelledAlerts,
            TotalReports = recentReports.Count,
            TotalAwarenessReports = recentReports.Count(r => r.Type == ReportType.Awareness),
            TotalInfoReports = recentReports.Count(r => r.Type == ReportType.Info)
        };
    }

    public async Task<AlertMetrics> GetAlertMetricsAsync(string alertId, GeoPoint lastKnownLocation)
    {
        List<CitizenReport> reports = await _reportsRepository.GetReportsSinceAsync(DateTime.MinValue, DateTime.UtcNow);
        List<CitizenReport> alertReports = reports.Where(r => r.AlertId == alertId).ToList();

        int awarenessCount = alertReports.Count(r => r.Type == ReportType.Awareness);
        int infoCount = alertReports.Count(r => r.Type == ReportType.Info);

        // Alcance geográfico: distancia máxima desde el último punto conocido a cualquier reporte de tipo Info.
        double maxReachKm = 0;
        foreach (CitizenReport report in alertReports.Where(r => r.Type == ReportType.Info))
        {
            double distanceKm = DistanceCalculator.CalculateDistanceKm(
                lastKnownLocation.Latitude, lastKnownLocation.Longitude,
                report.Latitude, report.Longitude);
            if (distanceKm > maxReachKm) maxReachKm = distanceKm;
        }

        return new AlertMetrics
        {
            TotalAwareness = awarenessCount,
            TotalInfoReports = infoCount,
            GeographicReachKm = Math.Round(maxReachKm, 2)
        };
    }
}
