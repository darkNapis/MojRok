using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MojRok.Application.Deadlines;
using MojRok.Application.Deadlines.DTOs;
using MojRok.Application.Exceptions;

namespace MojRok.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DeadlinesController : ControllerBase
{
    private readonly DeadlineService _deadlineService;

    public DeadlinesController(DeadlineService deadlineService)
    {
        _deadlineService = deadlineService;
    }

    /// <summary>Returns all deadlines for the authenticated user, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DeadlineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDeadlines(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var deadlines = await _deadlineService.GetAllAsync(userId.Value, ct);
        return Ok(deadlines);
    }

    /// <summary>Returns a single deadline belonging to the authenticated user.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeadlineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeadline(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var deadline = await _deadlineService.GetByIdAsync(userId.Value, id, ct);
            return Ok(deadline);
        }
        catch (NotFoundException) { return NotFound(); }
    }

    /// <summary>Creates a new deadline for the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeadlineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDeadline(
        [FromBody] CreateDeadlineRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var deadline = await _deadlineService.CreateAsync(userId.Value, request, ct);
            return CreatedAtAction(nameof(GetDeadline), new { id = deadline.Id }, deadline);
        }
        catch (NotFoundException ex)    { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex)    { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// Updates an existing deadline. Only the owner can update.
    /// IsCompleted/CompletedAt are managed via POST /{id}/complete.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DeadlineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDeadline(
        Guid id,
        [FromBody] UpdateDeadlineRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var deadline = await _deadlineService.UpdateAsync(userId.Value, id, request, ct);
            return Ok(deadline);
        }
        catch (NotFoundException ex)    { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex)    { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Deletes the authenticated user's deadline. Returns 404 if not found or not owned.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDeadline(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await _deadlineService.DeleteAsync(userId.Value, id, ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
    }

    /// <summary>
    /// Marks the authenticated user's deadline as completed.
    /// Idempotent: safe to call multiple times.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(DeadlineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteDeadline(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var deadline = await _deadlineService.CompleteAsync(userId.Value, id, ct);
            return Ok(deadline);
        }
        catch (NotFoundException) { return NotFound(); }
    }

    /// <summary>
    /// Extracts the authenticated user's Guid from the JWT.
    /// Checks "sub" first (JwtBearer with MapInboundClaims=false or cleared DefaultInboundClaimTypeMap),
    /// then ClaimTypes.NameIdentifier as a fallback for older handler behaviour.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
