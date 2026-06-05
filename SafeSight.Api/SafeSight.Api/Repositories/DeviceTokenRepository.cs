using Microsoft.EntityFrameworkCore;
using SafeSight.Api.Data.Mongo;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Repositories;

public class DeviceTokenRepository : IDeviceTokenRepository
{
    private readonly SafeSightDbContext _context;

    public DeviceTokenRepository(SafeSightDbContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(string token)
    {
        bool exists = await _context.DeviceTokens.AnyAsync(t => t.Token == token);
        if (!exists)
        {
            _context.DeviceTokens.Add(new DeviceToken { Token = token });
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetAllAsync()
    {
        return await _context.DeviceTokens
            .Select(t => t.Token)
            .ToListAsync();
    }

    public async Task RemoveAsync(string token)
    {
        DeviceToken? existing = await _context.DeviceTokens.FirstOrDefaultAsync(t => t.Token == token);
        if (existing != null)
        {
            _context.DeviceTokens.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }
}
