using Microsoft.EntityFrameworkCore;
using SafeSight.Api.Data.Mongo;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Repositories;

public class AdminActivityRepository : IAdminActivityRepository
{
    private readonly SafeSightDbContext _context;

    public AdminActivityRepository(SafeSightDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminActivity activity)
    {
        _context.AdminActivities.Add(activity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AdminActivity>> GetRecentAsync(int limit)
    {
        return await _context.AdminActivities
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
