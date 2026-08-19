using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.DTOs.Auth;
using MIC.risk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using MIC.risk.Extensions;

namespace MIC.risk.Controllers;

[Authorize]
[ApiController]
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetAllAsync(cancellationToken);
        return Ok(employees);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            return this.NotFoundProblem($"Employee with ID {id} was not found.");
        }

        return Ok(employee);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var createdEmployee = await _employeeService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = createdEmployee.Id },
                createdEmployee);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updatedEmployee = await _employeeService.UpdateAsync(id, dto, cancellationToken);
            if (updatedEmployee == null)
            {
                return this.NotFoundProblem($"Employee with ID {id} was not found.");
            }

            return Ok(updatedEmployee);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPatch("{id:long}/toggle-active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken cancellationToken)
    {
        var success = await _employeeService.ToggleActiveStatusAsync(id, cancellationToken);
        if (!success)
        {
            return this.NotFoundProblem($"Employee with ID {id} was not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Sets a new password for an employee who cannot sign in. There is no self-service reset,
    /// so this is the only recovery path for a forgotten password.
    /// </summary>
    [HttpPost("{id:long}/reset-password")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        long id,
        [FromBody] ResetPasswordDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var success = await _employeeService.ResetPasswordAsync(id, dto.NewPassword, cancellationToken);
            if (!success)
            {
                return this.NotFoundProblem($"Employee with ID {id} was not found.");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }
}