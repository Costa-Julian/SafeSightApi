using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Repositories.Interfaces;

public interface IHeatmapRepository
{
    Task<List<HeatmapCell>> GetByAlertIdAsync(string? alertId);
    Task UpsertAsync(HeatmapCell cell);
}
