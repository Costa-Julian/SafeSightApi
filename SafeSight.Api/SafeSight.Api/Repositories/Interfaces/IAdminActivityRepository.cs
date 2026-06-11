using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Repositories.Interfaces;

public interface IAdminActivityRepository
{
    Task AddAsync(AdminActivity activity);
    Task<List<AdminActivity>> GetRecentAsync(int limit);
}
