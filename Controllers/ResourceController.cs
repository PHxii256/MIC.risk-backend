using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Extensions;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Controllers;

[Authorize]
[ApiController]
[Route("api/resource")]
public class ResourceController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly IEmployeeService _employeeService;

    public ResourceController(
        IResourceService resourceService,
        IEmployeeService employeeService)
    {
        _resourceService = resourceService;
        _employeeService = employeeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResourceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var resources = await _resourceService.GetAllAsync(cancellationToken);
        return Ok(resources);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ResourceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(id, cancellationToken);
        if (resource == null)
        {
            return this.NotFoundProblem($"Resource with ID {id} was not found.");
        }

        return Ok(resource);
    }

    [HttpGet("{id:long}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        long id,
        CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(id, cancellationToken);

        if (resource == null)
        {
            return this.NotFoundProblem($"Resource with ID {id} was not found.");
        }

        // Only server-stored resources should reach this endpoint.
        if (resource.Type is not ("File" or "Image"))
        {
            return this.BadRequestProblem(
                "This resource is not a server-stored file.");
        }

        // Get the actual file from your storage.
        var file = await _resourceService.GetFileAsync(id, cancellationToken);

        if (file == null)
        {
            return this.NotFoundProblem("The resource file was not found.");
        }

        return File(
            file.Content,
            file.ContentType,
            file.FileName);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ResourceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateResourceRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _resourceService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10485760)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResourceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string name,
        [FromForm] string? description,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await RequireActiveAdminEmployeeAsync(cancellationToken);
            var created = await _resourceService.UploadAsync(
                employee.Id,
                name,
                file,
                description,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPatch("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ResourceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(long id, [FromBody] PatchResourceRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _resourceService.PatchAsync(id, dto, cancellationToken);
            if (updated == null)
            {
                return this.NotFoundProblem($"Resource with ID {id} was not found.");
            }

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var success = await _resourceService.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            return this.NotFoundProblem($"Resource with ID {id} was not found.");
        }

        return NoContent();
    }

    private async Task<EmployeeResponseDto> RequireActiveAdminEmployeeAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _employeeService.EnsureActiveByIdentityUserIdAsync(userId, cancellationToken);
        return (await _employeeService.GetByIdentityUserIdAsync(userId, cancellationToken))!;
    }
}
