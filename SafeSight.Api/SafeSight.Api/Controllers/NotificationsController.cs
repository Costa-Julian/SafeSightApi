using Microsoft.AspNetCore.Mvc;
using SafeSight.Api.Common;
using SafeSight.Api.Models.Dtos.Requests;

namespace SafeSight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(ILogger<NotificationsController> logger)
    {
        _logger = logger;
    }

    [HttpPost("register-token")]
    public ActionResult<ApiResult<bool>> RegisterToken([FromBody] RegisterTokenRequest request)
    {
        // MVP: registra el token en el log. En producción: persistir en base de datos
        // y usar para enviar notificaciones push vía FCM.
        _logger.LogInformation("Token FCM registrado: role={Role}", request.Role);
        return Ok(ApiResult<bool>.Ok(true));
    }
}
