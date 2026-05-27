using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Requests;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Services;

public class AlertsService : IAlertsService
{
    private readonly IAlertsRepository _alertsRepository;
    private readonly IStatsRepository _statsRepository;
    private readonly IPhotoStorageService _photoStorageService;

    public AlertsService(IAlertsRepository alertsRepository, IStatsRepository statsRepository, IPhotoStorageService photoStorageService)
    {
        _alertsRepository = alertsRepository;
        _statsRepository = statsRepository;
        _photoStorageService = photoStorageService;
    }

    public async Task<PagedResponse<AlertResponse>> GetActiveAsync(int page, int pageSize)
    {
        PagedResponse<Alert> paged = await _alertsRepository.GetActiveAsync(page, pageSize);
        return new PagedResponse<AlertResponse>
        {
            Items = paged.Items.Select(AlertResponse.FromDomain).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
            TotalPages = paged.TotalPages
        };
    }

    public async Task<AlertResponse?> GetByIdAsync(string id)
    {
        Alert? alert = await _alertsRepository.GetByIdAsync(id);
        return alert == null ? null : AlertResponse.FromDomain(alert);
    }

    public async Task<AlertMetricsResponse?> GetMetricsAsync(string id)
    {
        Alert? alert = await _alertsRepository.GetByIdAsync(id);
        if (alert == null) return null;

        AlertMetrics metrics = await _statsRepository.GetAlertMetricsAsync(id, alert.LastKnownLocation);
        return AlertMetricsResponse.FromDomain(id, metrics);
    }

    public async Task<PagedResponse<AlertResponse>> GetByEmitterAsync(int emitterId, int page, int pageSize)
    {
        PagedResponse<Alert> paged = await _alertsRepository.GetByEmitterAsync(emitterId, page, pageSize);
        return new PagedResponse<AlertResponse>
        {
            Items = paged.Items.Select(AlertResponse.FromDomain).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
            TotalPages = paged.TotalPages
        };
    }

    public async Task<AlertResponse> CreateAsync(CreateAlertRequest request)
    {
        string photoUrl = string.Empty;
        if (request.Photo != null)
        {
            photoUrl = await _photoStorageService.SaveAsync(request.Photo);
        }

        Alert alert = new Alert
        {
            MissingPerson = new MissingPerson
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Age = request.Age,
                PhysicalDescription = request.PhysicalDescription,
                PhotoUrl = photoUrl
            },
            Situation = request.Situation,
            LastKnownLocation = new GeoPoint
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude
            },
            DisappearanceDate = request.DisappearanceDate,
            EmitterId = request.EmitterId,
            EmittedAt = DateTime.UtcNow,
            Status = AlertStatus.Active
        };

        Alert created = await _alertsRepository.CreateAsync(alert);
        return AlertResponse.FromDomain(created);
    }

    public async Task<bool> UpdateStatusAsync(string id, UpdateAlertStatusRequest request)
    {
        return await _alertsRepository.UpdateStatusAsync(id, request.Status);
    }
}
