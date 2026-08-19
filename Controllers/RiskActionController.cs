using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Services.Interfaces;
using MIC.risk.Extensions;

namespace MIC.risk.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/risk-action")]
public class RiskActionController : ControllerBase
{
    private readonly IRiskActionService _riskActionService;

    public RiskActionController(IRiskActionService riskActionService)
    {
        _riskActionService = riskActionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<RiskActionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationHelper.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _riskActionService.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(RiskActionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _riskActionService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(RiskActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var action = await _riskActionService.GetByIdAsync(id, cancellationToken);
        if (action == null)
        {
            return this.NotFoundProblem($"Risk action with ID {id} was not found.");
        }

        return Ok(action);
    }

    [HttpGet("by-report/{reportId:long}")]
    [ProducesResponseType(typeof(IEnumerable<RiskActionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByReport(long reportId, CancellationToken cancellationToken)
    {
        var actions = await _riskActionService.GetByReportIdAsync(reportId, cancellationToken);
        return Ok(actions);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RiskActionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRiskActionRequestDto dto, CancellationToken cancellationToken)
    {
        var created = await _riskActionService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(RiskActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRiskActionRequestDto dto, CancellationToken cancellationToken)
    {
        var updated = await _riskActionService.UpdateAsync(id, dto, cancellationToken);
        if (updated == null)
        {
            return this.NotFoundProblem($"Risk action with ID {id} was not found.");
        }

        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var success = await _riskActionService.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            return this.NotFoundProblem($"Risk action with ID {id} was not found.");
        }

        return NoContent();
    }
}
