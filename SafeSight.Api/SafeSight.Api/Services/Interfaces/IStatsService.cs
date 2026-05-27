using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Services.Interfaces;

public interface IStatsService
{
    Task<StatsOverviewResponse> GetOverviewAsync();
    Task<HeatmapResponse?> GetHeatmapAsync(string? alertId);
}
