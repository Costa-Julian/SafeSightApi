using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Requests;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Services;

public class ReportsService : IReportsService
{
    private readonly IReportsRepository _reportsRepository;
    private readonly IPhotoStorageService _photoStorageService;
    private readonly IAlertsRepository _alertsRepository;
    private readonly IAdminActivityRepository _activityRepository;

    public ReportsService(
        IReportsRepository reportsRepository,
        IPhotoStorageService photoStorageService,
        IAlertsRepository alertsRepository,
        IAdminActivityRepository activityRepository)
    {
        _reportsRepository = reportsRepository;
        _photoStorageService = photoStorageService;
        _alertsRepository = alertsRepository;
        _activityRepository = activityRepository;
    }

    public async Task<CitizenReport> CreateAwarenessAsync(AwarenessReportRequest request)
    {
        CitizenReport report = new CitizenReport
        {
            Id = Guid.NewGuid(),
            AlertId = request.AlertId,
            Type = ReportType.Awareness,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ReportedAt = request.ReportedAt.ToUniversalTime()
        };

        await _reportsRepository.InsertAsync(report);
        await WriteReportActivityAsync(report.AlertId, "Nuevo reporte de avistamiento");
        return report;
    }

    public async Task<CitizenReport> CreateInfoAsync(InfoReportRequest request)
    {
        string? photoUrl = null;
        if (request.Photo != null)
        {
            photoUrl = await _photoStorageService.SaveAsync(request.Photo);
        }

        CitizenReport report = new CitizenReport
        {
            Id = Guid.NewGuid(),
            AlertId = request.AlertId,
            CitizenId = request.CitizenId,
            Type = ReportType.Info,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ReportedAt = request.ReportedAt.ToUniversalTime(),
            Description = request.Description,
            PhotoUrl = photoUrl
        };

        await _reportsRepository.InsertAsync(report);
        await WriteReportActivityAsync(report.AlertId, "Nuevo reporte de información");
        return report;
    }

    public async Task<PagedResponse<CitizenReport>> GetByAlertAsync(string alertId, int page, int pageSize)
    {
        return await _reportsRepository.GetByAlertAsync(alertId, page, pageSize);
    }

    private async Task WriteReportActivityAsync(string alertId, string title)
    {
        Alert? alert = await _alertsRepository.GetByIdAsync(alertId);
        string name = alert != null ? $"{alert.MissingPerson.FirstName} {alert.MissingPerson.LastName}" : alertId;

        await _activityRepository.AddAsync(new AdminActivity
        {
            Id = Guid.NewGuid().ToString(),
            Kind = "ReportSubmitted",
            Title = title,
            Description = $"Un ciudadano aportó información sobre la alerta de {name}.",
            Timestamp = DateTime.UtcNow,
            AlertId = alertId
        });
    }
}
