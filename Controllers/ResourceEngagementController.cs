using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Extensions;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Controllers;

[Authorize]
[ApiController]
[Route("api/resource-engagement")]
public class ResourceEngagementController : ControllerBase
{
    private readonly IResourceEngagementService _engagementService;
    private readonly IEmployeeService _employeeService;

    public ResourceEngagementController(
        IResourceEngagementService engagementService,
        IEmployeeService employeeService)
    {
        _engagementService = engagementService;
        _employeeService = employeeService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResourceEngagementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert([FromBody] RecordResourceEngagementRequestDto dto, CancellationToken cancellationToken)
    {
        var employee = await RequireActiveEmployeeAsync(cancellationToken);

        if (!User.IsAdmin() && dto.EmpId != employee.Id)
        {
            return Forbid();
        }

        await _employeeService.EnsureActiveByIdAsync(dto.EmpId, cancellationToken);

        var result = await _engagementService.UpsertAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<ResourceEngagementResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var employee = await RequireActiveEmployeeAsync(cancellationToken);
        var engagements = await _engagementService.GetByEmployeeIdAsync(employee.Id, cancellationToken);
        return Ok(engagements);
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<ResourceEngagementStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _engagementService.GetResourceStatsAsync(cancellationToken);
        return Ok(stats);
    }

    [HttpGet("by-department")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<DepartmentEngagementStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDepartment(CancellationToken cancellationToken)
    {
        var stats = await _engagementService.GetEngagementByDepartmentAsync(cancellationToken);
        return Ok(stats);
    }

    private async Task<EmployeeResponseDto> RequireActiveEmployeeAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _employeeService.EnsureActiveByIdentityUserIdAsync(userId, cancellationToken);
        return (await _employeeService.GetByIdentityUserIdAsync(userId, cancellationToken))!;
    }
}
