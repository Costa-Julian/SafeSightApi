using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Repositories.Interfaces;

public interface IInfoReportsMongoRepository
{
    Task UpsertAsync(InfoReportDocument doc);
    Task<PagedResponse<InfoReportDocument>> GetByAlertAsync(string alertId, int page, int pageSize);
}
