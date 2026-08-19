using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Services.Interfaces;
using MIC.risk.Extensions;

namespace MIC.risk.Controllers;

[Authorize]
[ApiController]
[Route("api/department")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DepartmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetAllAsync(cancellationToken);
        return Ok(departments);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(DepartmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetByIdAsync(id, cancellationToken);
        if (department == null)
        {
            return this.NotFoundProblem($"Department with ID {id} was not found.");
        }

        return Ok(department);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DepartmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _departmentService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DepartmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] CreateDepartmentRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _departmentService.UpdateAsync(id, dto, cancellationToken);
            if (updated == null)
            {
                return this.NotFoundProblem($"Department with ID {id} was not found.");
            }

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }
}
