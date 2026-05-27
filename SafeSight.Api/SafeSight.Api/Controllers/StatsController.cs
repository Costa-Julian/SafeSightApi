using Microsoft.AspNetCore.Mvc;
using SafeSight.Api.Common;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResult<StatsOverviewResponse>>> GetOverview()
    {
        StatsOverviewResponse stats = await _statsService.GetOverviewAsync();
        return Ok(ApiResult<StatsOverviewResponse>.Ok(stats));
    }

    [HttpGet("heatmap")]
    public async Task<ActionResult<ApiResult<HeatmapResponse>>> GetHeatmap([FromQuery] string? alertId)
    {
        HeatmapResponse? heatmap = await _statsService.GetHeatmapAsync(alertId);
        return Ok(ApiResult<HeatmapResponse>.Ok(heatmap!));
    }
}
