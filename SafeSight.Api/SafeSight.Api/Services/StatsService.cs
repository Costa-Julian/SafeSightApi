using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Services;

public class StatsService : IStatsService
{
    private readonly IStatsRepository _statsRepository;
    private readonly IHeatmapRepository _heatmapRepository;

    public StatsService(IStatsRepository statsRepository, IHeatmapRepository heatmapRepository)
    {
        _statsRepository = statsRepository;
        _heatmapRepository = heatmapRepository;
    }

    public async Task<StatsOverviewResponse> GetOverviewAsync()
    {
        return await _statsRepository.GetOverviewAsync();
    }

    public async Task<HeatmapResponse?> GetHeatmapAsync(string? alertId)
    {
        // El heatmap SIEMPRE se lee de la colección pre-agregada en MongoDB.
        // Nunca se calcula al vuelo: esto es un Data Mart optimizado para consulta analítica.
        // El HeatmapSyncService mantiene estos datos actualizados con consistencia eventual.
        List<HeatmapCell> cells = await _heatmapRepository.GetByAlertIdAsync(alertId);

        return new HeatmapResponse
        {
            AlertId = alertId,
            Cells = cells.Select(HeatmapCellDto.FromDomain).ToList()
        };
    }
}
