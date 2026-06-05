namespace SafeSight.Api.Repositories.Interfaces;

public interface IDeviceTokenRepository
{
    Task UpsertAsync(string token);
    Task<List<string>> GetAllAsync();
    Task RemoveAsync(string token);
}
