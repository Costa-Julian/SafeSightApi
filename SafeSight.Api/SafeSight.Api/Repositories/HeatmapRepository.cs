using Microsoft.EntityFrameworkCore;
using SafeSight.Api.Data.Mongo;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Repositories;

public class HeatmapRepository : IHeatmapRepository
{
    private readonly SafeSightDbContext _context;

    public HeatmapRepository(SafeSightDbContext context)
    {
        _context = context;
    }

    public async Task<List<HeatmapCell>> GetByAlertIdAsync(string? alertId)
    {
        if (alertId == null)
        {
            return await _context.HeatmapCells
                .Where(c => c.AlertId == null)
                .ToListAsync();
        }

        return await _context.HeatmapCells
            .Where(c => c.AlertId == alertId)
            .ToListAsync();
    }

    public async Task UpsertAsync(HeatmapCell cell)
    {
        HeatmapCell? existing = await _context.HeatmapCells
            .FirstOrDefaultAsync(c => c.Id == cell.Id);

        if (existing == null)
        {
            _context.HeatmapCells.Add(cell);
        }
        else
        {
            // Acumular conteos (el sync es incremental)
            existing.AwarenessCount += cell.AwarenessCount;
            existing.InfoCount += cell.InfoCount;
            existing.WeightedIntensity += cell.WeightedIntensity;
            existing.LastUpdated = cell.LastUpdated;
        }

        await _context.SaveChangesAsync();
    }
}
