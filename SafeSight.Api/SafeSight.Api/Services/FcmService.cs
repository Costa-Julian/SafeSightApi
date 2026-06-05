using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Services;

public class FcmService : IFcmService
{
    private readonly IDeviceTokenRepository _tokenRepository;
    private readonly ILogger<FcmService> _logger;

    public FcmService(IDeviceTokenRepository tokenRepository, ILogger<FcmService> logger)
    {
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    public async Task SendNewAlertAsync(AlertResponse alert)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("Firebase no está inicializado. Omitiendo notificación push.");
            return;
        }

        List<string> tokens = await _tokenRepository.GetAllAsync();
        if (tokens.Count == 0) return;

        string title = $"Nueva alerta: {alert.FirstName} {alert.LastName}";
        string body = $"{alert.Age} años — {alert.Situation}";

        // FCM permite hasta 500 tokens por llamada
        foreach (string[] chunk in tokens.Chunk(500))
        {
            MulticastMessage message = new MulticastMessage
            {
                Tokens = chunk.ToList(),
                Notification = new Notification { Title = title, Body = body },
                Data = new Dictionary<string, string>
                {
                    { "alertId", alert.Id },
                    { "type", "new_alert" }
                },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification { Sound = "default" }
                }
            };

            try
            {
                BatchResponse response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation("FCM: {Success}/{Total} notificaciones enviadas", response.SuccessCount, chunk.Length);

                // Limpia tokens inválidos (dispositivos que desinstalaron la app)
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    SendResponse r = response.Responses[i];
                    if (!r.IsSuccess && r.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered)
                    {
                        await _tokenRepository.RemoveAsync(chunk[i]);
                        _logger.LogInformation("Token obsoleto removido: {Token}...", chunk[i][..20]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando notificaciones FCM");
            }
        }
    }
}
