using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Requests;
using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Services.Interfaces;

public interface IAlertsService
{
    Task<PagedResponse<AlertResponse>> GetActiveAsync(int page, int pageSize);
    Task<AlertResponse?> GetByIdAsync(string id);
    Task<AlertMetricsResponse?> GetMetricsAsync(string id);
    Task<PagedResponse<AlertResponse>> GetByEmitterAsync(int emitterId, int page, int pageSize);
    Task<AlertResponse> CreateAsync(CreateAlertRequest request);
    Task<bool> UpdateStatusAsync(string id, UpdateAlertStatusRequest request);
}
