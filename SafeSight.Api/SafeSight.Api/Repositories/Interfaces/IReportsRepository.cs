using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Repositories.Interfaces;

public interface IReportsRepository
{
    Task InsertAsync(CitizenReport report);
    Task<PagedResponse<CitizenReport>> GetByAlertAsync(string alertId, int page, int pageSize);
    Task<List<CitizenReport>> GetReportsSinceAsync(DateTime since, DateTime until);
    Task<long> CountByAlertAsync(string alertId);
}
