using SafeSight.Api.Models.Domain;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Seed;

public class ActivityMigration
{
    private readonly IAlertsRepository _alertsRepository;
    private readonly IAdminActivityRepository _activityRepository;
    private readonly ILogger<ActivityMigration> _logger;

    public ActivityMigration(
        IAlertsRepository alertsRepository,
        IAdminActivityRepository activityRepository,
        ILogger<ActivityMigration> logger)
    {
        _alertsRepository = alertsRepository;
        _activityRepository = activityRepository;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        List<AdminActivity> existing = await _activityRepository.GetRecentAsync(1);
        if (existing.Count > 0)
        {
            _logger.LogInformation("ActivityMigration: colección ya tiene datos, se omite.");
            return;
        }

        List<Alert> alerts = await _alertsRepository.GetAllAsync();
        if (alerts.Count == 0)
        {
            _logger.LogInformation("ActivityMigration: no hay alertas para migrar.");
            return;
        }

        foreach (Alert alert in alerts)
        {
            await _activityRepository.AddAsync(new AdminActivity
            {
                Id = Guid.NewGuid().ToString(),
                Kind = "AlertCreated",
                Title = "Nueva alerta emitida",
                Description = $"Se emitió una alerta para {alert.MissingPerson.FirstName} {alert.MissingPerson.LastName} ({alert.MissingPerson.Age} años).",
                Timestamp = alert.EmittedAt,
                AlertId = alert.Id
            });
        }

        _logger.LogInformation("ActivityMigration: generados {Count} eventos de actividad.", alerts.Count);
    }
}
