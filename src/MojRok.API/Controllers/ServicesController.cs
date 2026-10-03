using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MojRok.Application.Exceptions;
using MojRok.Application.Services;
using MojRok.Application.Services.DTOs;

namespace MojRok.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly CitizenServiceService _service;

    public ServicesController(CitizenServiceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns active citizen services.
    /// Optional filters: categoryId, municipalityId.
    /// Public endpoint - no auth required.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CitizenServiceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] int? municipalityId,
        CancellationToken ct)
    {
        var list = await _service.GetAllAsync(categoryId, municipalityId, ct);
        return Ok(list);
    }

    /// <summary>Returns all service categories. Public.</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ServiceCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        return Ok(await _service.GetCategoriesAsync(ct));
    }

    /// <summary>Returns a single citizen service by id. Public.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CitizenServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(id, ct));
        }
        catch (NotFoundException) { return NotFound(); }
    }

    /// <summary>Admin only - creates a new citizen service.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CitizenServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCitizenServiceRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Admin only - updates an existing citizen service.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CitizenServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCitizenServiceRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            return Ok(await _service.UpdateAsync(id, request, ct));
        }
        catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Admin only - deletes a citizen service.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
    }
}