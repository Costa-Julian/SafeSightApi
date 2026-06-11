using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Services.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardResponse> GetDashboardAsync();
    Task<List<AdminActivityItem>> GetActivityAsync(int limit);
    Task<List<AlertResponse>> GetAllAlertsAsync();
    Task<List<AdminReportRow>> GetReportsAsync(int limit);
}
