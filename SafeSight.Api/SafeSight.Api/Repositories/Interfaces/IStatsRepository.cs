using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Repositories.Interfaces;

public interface IStatsRepository
{
    Task<StatsOverviewResponse> GetOverviewAsync();
    Task<AlertMetrics> GetAlertMetricsAsync(string alertId, GeoPoint lastKnownLocation);
}
