using Microsoft.AspNetCore.Mvc;
using SafeSight.Api.Common;
using SafeSight.Api.Models.Dtos.Requests;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertsService _alertsService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IAlertsService alertsService, ILogger<AlertsController> logger)
    {
        _alertsService = alertsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<PagedResponse<AlertResponse>>>> GetActive(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        PagedResponse<AlertResponse> result = await _alertsService.GetActiveAsync(page, pageSize);
        return Ok(ApiResult<PagedResponse<AlertResponse>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResult<AlertResponse>>> GetById(string id)
    {
        AlertResponse? alert = await _alertsService.GetByIdAsync(id);
        if (alert == null)
            return NotFound(ApiResult<AlertResponse>.Fail("Alerta no encontrada."));
        return Ok(ApiResult<AlertResponse>.Ok(alert));
    }

    [HttpGet("{id}/metrics")]
    public async Task<ActionResult<ApiResult<AlertMetricsResponse>>> GetMetrics(string id)
    {
        AlertMetricsResponse? metrics = await _alertsService.GetMetricsAsync(id);
        if (metrics == null)
            return NotFound(ApiResult<AlertMetricsResponse>.Fail("Alerta no encontrada."));
        return Ok(ApiResult<AlertMetricsResponse>.Ok(metrics));
    }

    [HttpGet("by-emitter/{emitterId:int}")]
    public async Task<ActionResult<ApiResult<PagedResponse<AlertResponse>>>> GetByEmitter(
        int emitterId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        PagedResponse<AlertResponse> result = await _alertsService.GetByEmitterAsync(emitterId, page, pageSize);
        return Ok(ApiResult<PagedResponse<AlertResponse>>.Ok(result));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResult<AlertResponse>>> Create([FromForm] CreateAlertRequest request)
    {
        AlertResponse created = await _alertsService.CreateAsync(request);
        _logger.LogInformation("Alerta creada: {Id}", created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResult<AlertResponse>.Ok(created));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResult<bool>>> UpdateStatus(string id, [FromBody] UpdateAlertStatusRequest request)
    {
        bool updated = await _alertsService.UpdateStatusAsync(id, request);
        if (!updated)
            return NotFound(ApiResult<bool>.Fail("Alerta no encontrada."));
        return Ok(ApiResult<bool>.Ok(true));
    }
}
