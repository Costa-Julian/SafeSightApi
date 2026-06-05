using Microsoft.AspNetCore.Mvc;
using SafeSight.Api.Common;
using SafeSight.Api.Models.Dtos.Requests;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IDeviceTokenRepository _tokenRepository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IDeviceTokenRepository tokenRepository, ILogger<NotificationsController> logger)
    {
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    [HttpPost("register-token")]
    public async Task<ActionResult<ApiResult<bool>>> RegisterToken([FromBody] RegisterTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(ApiResult<bool>.Fail("Token inválido"));

        await _tokenRepository.UpsertAsync(request.Token);
        _logger.LogInformation("Token FCM registrado/actualizado. Role={Role}", request.Role);
        return Ok(ApiResult<bool>.Ok(true));
    }
}
