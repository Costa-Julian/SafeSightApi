using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Repositories.Interfaces;

public interface IAlertsRepository
{
    Task<PagedResponse<Alert>> GetActiveAsync(int page, int pageSize);
    Task<Alert?> GetByIdAsync(string id);
    Task<PagedResponse<Alert>> GetByEmitterAsync(int emitterId, int page, int pageSize);
    Task<Alert> CreateAsync(Alert alert);
    Task<bool> UpdateStatusAsync(string id, AlertStatus status);
    Task<int> CountActiveAsync();
    Task<int> CountByStatusAsync(AlertStatus status);
    Task<int> CountTotalAsync();
}
