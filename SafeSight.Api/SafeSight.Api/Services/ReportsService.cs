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

    public ReportsService(IReportsRepository reportsRepository, IPhotoStorageService photoStorageService)
    {
        _reportsRepository = reportsRepository;
        _photoStorageService = photoStorageService;
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
        return report;
    }

    public async Task<PagedResponse<CitizenReport>> GetByAlertAsync(string alertId, int page, int pageSize)
    {
        return await _reportsRepository.GetByAlertAsync(alertId, page, pageSize);
    }
}
