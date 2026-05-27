using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Requests;
using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Services.Interfaces;

public interface IReportsService
{
    Task<CitizenReport> CreateAwarenessAsync(AwarenessReportRequest request);
    Task<CitizenReport> CreateInfoAsync(InfoReportRequest request);
    Task<PagedResponse<CitizenReport>> GetByAlertAsync(string alertId, int page, int pageSize);
}
