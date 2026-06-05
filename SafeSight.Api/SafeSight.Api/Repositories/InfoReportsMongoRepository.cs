using Microsoft.EntityFrameworkCore;
using SafeSight.Api.Data.Mongo;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Repositories;

public class InfoReportsMongoRepository : IInfoReportsMongoRepository
{
    private readonly SafeSightDbContext _context;

    public InfoReportsMongoRepository(SafeSightDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<InfoReportDocument>> GetByAlertAsync(string alertId, int page, int pageSize)
    {
        IQueryable<InfoReportDocument> query = _context.InfoReports
            .Where(r => r.AlertId == alertId)
            .OrderByDescending(r => r.ReportedAt);

        int total = await query.CountAsync();
        List<InfoReportDocument> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<InfoReportDocument>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize)
        };
    }

    public async Task UpsertAsync(InfoReportDocument doc)
    {
        InfoReportDocument? existing = await _context.InfoReports
            .FirstOrDefaultAsync(r => r.Id == doc.Id);

        if (existing == null)
            _context.InfoReports.Add(doc);
        else
        {
            existing.AlertId = doc.AlertId;
            existing.CitizenId = doc.CitizenId;
            existing.Latitude = doc.Latitude;
            existing.Longitude = doc.Longitude;
            existing.Description = doc.Description;
            existing.PhotoUrl = doc.PhotoUrl;
            existing.ReportedAt = doc.ReportedAt;
        }

        await _context.SaveChangesAsync();
    }
}
