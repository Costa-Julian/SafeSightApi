using Microsoft.AspNetCore.Mvc;
using SafeSight.Api.Common;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Requests;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportsService reportsService, ILogger<ReportsController> logger)
    {
        _reportsService = reportsService;
        _logger = logger;
    }

    [HttpPost("awareness")]
    public async Task<ActionResult<ApiResult<CitizenReport>>> CreateAwareness([FromBody] AwarenessReportRequest request)
    {
        CitizenReport report = await _reportsService.CreateAwarenessAsync(request);
        _logger.LogInformation("Reporte awareness creado: {Id} para alerta {AlertId}", report.Id, report.AlertId);
        return Ok(ApiResult<CitizenReport>.Ok(report));
    }

    [HttpPost("info")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResult<CitizenReport>>> CreateInfo([FromForm] InfoReportRequest request)
    {
        CitizenReport report = await _reportsService.CreateInfoAsync(request);
        _logger.LogInformation("Reporte info creado: {Id} para alerta {AlertId}", report.Id, report.AlertId);
        return Ok(ApiResult<CitizenReport>.Ok(report));
    }

    [HttpGet("by-alert/{alertId}")]
    public async Task<ActionResult<ApiResult<PagedResponse<CitizenReport>>>> GetByAlert(
        string alertId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        PagedResponse<CitizenReport> result = await _reportsService.GetByAlertAsync(alertId, page, pageSize);
        return Ok(ApiResult<PagedResponse<CitizenReport>>.Ok(result));
    }
}
