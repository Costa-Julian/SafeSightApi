using Microsoft.AspNetCore.Mvc;
using SafeSight.Api.Common;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResult<AdminDashboardResponse>>> GetDashboard()
    {
        AdminDashboardResponse dashboard = await _adminService.GetDashboardAsync();
        return Ok(ApiResult<AdminDashboardResponse>.Ok(dashboard));
    }

    [HttpGet("activity")]
    public async Task<ActionResult<ApiResult<List<AdminActivityItem>>>> GetActivity(
        [FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 100);
        List<AdminActivityItem> items = await _adminService.GetActivityAsync(limit);
        return Ok(ApiResult<List<AdminActivityItem>>.Ok(items));
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResult<List<AlertResponse>>>> GetAllAlerts()
    {
        List<AlertResponse> alerts = await _adminService.GetAllAlertsAsync();
        return Ok(ApiResult<List<AlertResponse>>.Ok(alerts));
    }

    [HttpGet("reports")]
    public async Task<ActionResult<ApiResult<List<AdminReportRow>>>> GetReports(
        [FromQuery] int limit = 1000)
    {
        limit = Math.Clamp(limit, 1, 5000);
        List<AdminReportRow> rows = await _adminService.GetReportsAsync(limit);
        return Ok(ApiResult<List<AdminReportRow>>.Ok(rows));
    }
}
