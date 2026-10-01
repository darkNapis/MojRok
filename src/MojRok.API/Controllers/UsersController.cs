using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MojRok.Application.Exceptions;
using MojRok.Application.Users;
using MojRok.Application.Users.DTOs;

namespace MojRok.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _userService.GetMyProfileAsync(userId.Value, ct);
            return Ok(profile);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _userService.UpdateMyProfileAsync(userId.Value, request, ct);
            return Ok(profile);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Extracts the user Guid from the JWT.
    ///
    /// JwtBearer 9 uses JsonWebTokenHandler by default, which maps
    /// the "sub" claim to ClaimTypes.NameIdentifier. We check both
    /// to be resilient to handler changes and configuration differences.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
