using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MojRok.Application.Categories;
using MojRok.Application.Categories.DTOs;
using MojRok.Application.Exceptions;

namespace MojRok.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Returns system default categories and the current user's personal categories.
    /// Never returns another user's categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var categories = await _categoryService.GetCategoriesAsync(userId.Value, ct);
        return Ok(categories);
    }

    /// <summary>
    /// Creates a personal category for the current user.
    /// UserId is taken from JWT â€” never from the request body.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var response = await _categoryService.CreateCategoryAsync(userId.Value, request, ct);
        return CreatedAtAction(nameof(GetCategories), new { }, response);
    }

    /// <summary>
    /// Updates a personal category.
    /// Returns 403 if the category is a system default or belongs to another user.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var response = await _categoryService.UpdateCategoryAsync(userId.Value, id, request, ct);
            return Ok(response);
        }
        catch (NotFoundException)   { return NotFound(); }
        catch (ForbiddenException)  { return Forbid(); }
    }

    /// <summary>
    /// Deletes a personal category.
    /// Returns 403 if the category is a system default or belongs to another user.
    /// Returns 409 if the category has Deadlines assigned to it.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await _categoryService.DeleteCategoryAsync(userId.Value, id, ct);
            return NoContent();
        }
        catch (NotFoundException)   { return NotFound(); }
        catch (ForbiddenException)  { return Forbid(); }
        catch (ConflictException ex){ return Conflict(new { message = ex.Message }); }
    }

    /// <summary>
    /// Extracts the authenticated user's Guid from the JWT "sub" claim.
    /// Checks both the original "sub" name and the remapped ClaimTypes.NameIdentifier
    /// to be resilient across JwtBearer handler versions.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
