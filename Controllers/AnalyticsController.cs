using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AnalyticsDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var dashboard = await _analyticsService.GetDashboardAsync(from, to, cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("employees-by-department")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeDepartmentStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeesByDepartment(CancellationToken cancellationToken)
    {
        var stats = await _analyticsService.GetEmployeeStatsByDepartmentAsync(cancellationToken);
        return Ok(stats);
    }
}
