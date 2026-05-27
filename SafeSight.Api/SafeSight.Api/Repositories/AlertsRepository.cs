using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using SafeSight.Api.Data.Mongo;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Repositories;

public class AlertsRepository : IAlertsRepository
{
    private readonly SafeSightDbContext _context;

    public AlertsRepository(SafeSightDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<Alert>> GetActiveAsync(int page, int pageSize)
    {
        IQueryable<Alert> query = _context.Alerts.Where(a => a.Status == AlertStatus.Active);
        int total = await query.CountAsync();
        List<Alert> items = await query
            .OrderByDescending(a => a.EmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return BuildPagedResponse(items, page, pageSize, total);
    }

    public async Task<Alert?> GetByIdAsync(string id)
    {
        return await _context.Alerts.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<PagedResponse<Alert>> GetByEmitterAsync(int emitterId, int page, int pageSize)
    {
        IQueryable<Alert> query = _context.Alerts.Where(a => a.EmitterId == emitterId);
        int total = await query.CountAsync();
        List<Alert> items = await query
            .OrderByDescending(a => a.EmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return BuildPagedResponse(items, page, pageSize, total);
    }

    public async Task<Alert> CreateAsync(Alert alert)
    {
        alert.Id = ObjectId.GenerateNewId().ToString();
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();
        return alert;
    }

    public async Task<bool> UpdateStatusAsync(string id, AlertStatus status)
    {
        Alert? alert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == id);
        if (alert == null) return false;

        alert.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountActiveAsync()
    {
        return await _context.Alerts.CountAsync(a => a.Status == AlertStatus.Active);
    }

    public async Task<int> CountByStatusAsync(AlertStatus status)
    {
        return await _context.Alerts.CountAsync(a => a.Status == status);
    }

    public async Task<int> CountTotalAsync()
    {
        return await _context.Alerts.CountAsync();
    }

    private static PagedResponse<Alert> BuildPagedResponse(List<Alert> items, int page, int pageSize, int total)
    {
        return new PagedResponse<Alert>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize)
        };
    }
}
