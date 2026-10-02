# =============================================================
# MojRok — Phase 4: Categories
# Run from: C:\Users\APIS\OneDrive\Desktop\MojRok
# Prerequisites: Phase 1, 2, 3 completed and verified.
#
# NEW files:
#   Application/Exceptions/ForbiddenException.cs
#   Application/Exceptions/ConflictException.cs
#   Application/Categories/DTOs/CategoryResponse.cs
#   Application/Categories/DTOs/CreateCategoryRequest.cs
#   Application/Categories/DTOs/UpdateCategoryRequest.cs
#   Application/Categories/CategoryService.cs
#   Infrastructure/Persistence/DbSeeder.cs
#   API/Controllers/CategoriesController.cs
#   tests/MojRok.Tests/Categories/CategoryServiceTests.cs
#
# PATCHED files (targeted additions only):
#   API/Program.cs  (using + AddScoped + seeder call)
#
# No new NuGet packages.
# No migration (Category schema is unchanged from Phase 1).
# Default categories are seeded at startup via DbSeeder.
# No git commands.
# =============================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step  { param($msg) Write-Host "`n>>> $msg" -ForegroundColor Cyan }
function Write-Done  { param($msg) Write-Host "    [OK] $msg" -ForegroundColor Green }
function Write-Info  { param($msg) Write-Host "    [--] $msg" -ForegroundColor Gray }
function Write-Fatal { param($msg) Write-Host "`n[FATAL] $msg" -ForegroundColor Red; exit 1 }

if (-not (Test-Path 'MojRok.sln')) {
    Write-Fatal 'MojRok.sln not found. Run from C:\Users\APIS\OneDrive\Desktop\MojRok'
}
if (-not (Test-Path 'src\MojRok.API\Program.cs')) {
    Write-Fatal 'src\MojRok.API\Program.cs not found. Complete Phase 3 first.'
}

function Write-File {
    param([string]$Path, [string]$Content)
    $dir = Split-Path -Path $Path -Parent
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    Set-Content -Path $Path -Value $Content -Encoding UTF8
    Write-Done "  $Path"
}

Write-Host "`n=== MojRok Phase 4 — Categories ===" -ForegroundColor Yellow

# -----------------------------------------------------------
# STEP 1 — Application: new exception types
# -----------------------------------------------------------
Write-Step 'STEP 1 — Application exceptions'

Write-File 'src\MojRok.Application\Exceptions\ForbiddenException.cs' @'
namespace MojRok.Application.Exceptions;

/// <summary>
/// Thrown when the current user attempts an operation they are not
/// permitted to perform (e.g. modifying another user's category,
/// or editing a system default category).
/// Maps to HTTP 403 in the API layer.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
'@

Write-File 'src\MojRok.Application\Exceptions\ConflictException.cs' @'
namespace MojRok.Application.Exceptions;

/// <summary>
/// Thrown when an operation cannot be completed because of a
/// conflicting state (e.g. deleting a category that still has
/// Deadlines assigned to it).
/// Maps to HTTP 409 in the API layer.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
'@

# -----------------------------------------------------------
# STEP 2 — Application: Category DTOs
# -----------------------------------------------------------
Write-Step 'STEP 2 — Application: Category DTOs'

Write-File 'src\MojRok.Application\Categories\DTOs\CategoryResponse.cs' @'
namespace MojRok.Application.Categories.DTOs;

public class CategoryResponse
{
    public Guid    Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string  Color     { get; set; } = string.Empty;
    public string? IconSlug  { get; set; }
    public bool    IsDefault { get; set; }
    /// <summary>
    /// Null for system/default categories.
    /// Matches the authenticated user's Id for personal categories.
    /// </summary>
    public Guid?   UserId    { get; set; }
}
'@

Write-File 'src\MojRok.Application\Categories\DTOs\CreateCategoryRequest.cs' @'
using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Categories.DTOs;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Color { get; set; } = "#6366F1";

    [MaxLength(50)]
    public string? IconSlug { get; set; }
}
'@

Write-File 'src\MojRok.Application\Categories\DTOs\UpdateCategoryRequest.cs' @'
using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Categories.DTOs;

public class UpdateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Color { get; set; } = "#6366F1";

    [MaxLength(50)]
    public string? IconSlug { get; set; }
}
'@

# -----------------------------------------------------------
# STEP 3 — Application: CategoryService
# -----------------------------------------------------------
Write-Step 'STEP 3 — Application: CategoryService'

Write-File 'src\MojRok.Application\Categories\CategoryService.cs' @'
using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Application.Categories.DTOs;
using MojRok.Application.Exceptions;
using MojRok.Domain.Entities;

namespace MojRok.Application.Categories;

public class CategoryService
{
    private readonly IAppDbContext _db;

    public CategoryService(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns system default categories (UserId = null)
    /// plus the current user's personal categories.
    /// Never returns another user's categories.
    /// </summary>
    public async Task<List<CategoryResponse>> GetCategoriesAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var categories = await _db.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.IsDefault ? 0 : 1)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

        return categories.Select(ToResponse).ToList();
    }

    /// <summary>
    /// Creates a personal category for the current user.
    /// UserId is always taken from the JWT — never from the request.
    /// IsDefault is always false for user-created categories.
    /// </summary>
    public async Task<CategoryResponse> CreateCategoryAsync(
        Guid userId,
        CreateCategoryRequest request,
        CancellationToken ct = default)
    {
        var category = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = request.Name.Trim(),
            Color     = request.Color.Trim(),
            IconSlug  = request.IconSlug?.Trim(),
            UserId    = userId,
            IsDefault = false   // user-created categories are never defaults
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return ToResponse(category);
    }

    /// <summary>
    /// Updates a personal category.
    /// Forbidden if: category belongs to another user, or is a system default.
    /// </summary>
    public async Task<CategoryResponse> UpdateCategoryAsync(
        Guid userId,
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken ct = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null)
            throw new NotFoundException($"Category {categoryId} was not found.");

        if (category.IsDefault)
            throw new ForbiddenException("System default categories cannot be modified.");

        if (category.UserId != userId)
            throw new ForbiddenException("You can only modify your own categories.");

        category.Name     = request.Name.Trim();
        category.Color    = request.Color.Trim();
        category.IconSlug = request.IconSlug?.Trim();

        await _db.SaveChangesAsync(ct);
        return ToResponse(category);
    }

    /// <summary>
    /// Deletes a personal category.
    /// Forbidden if: category belongs to another user, or is a system default.
    /// Conflict if: category still has Deadlines assigned to it.
    /// </summary>
    public async Task DeleteCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken ct = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null)
            throw new NotFoundException($"Category {categoryId} was not found.");

        if (category.IsDefault)
            throw new ForbiddenException("System default categories cannot be deleted.");

        if (category.UserId != userId)
            throw new ForbiddenException("You can only delete your own categories.");

        // Check for assigned deadlines without loading them into memory
        var hasDeadlines = await _db.Deadlines
            .AnyAsync(d => d.CategoryId == categoryId, ct);

        if (hasDeadlines)
            throw new ConflictException(
                "This category cannot be deleted because it has deadlines assigned to it. " +
                "Reassign or delete those deadlines first.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        Id        = c.Id,
        Name      = c.Name,
        Color     = c.Color,
        IconSlug  = c.IconSlug,
        IsDefault = c.IsDefault,
        UserId    = c.UserId
    };
}
'@

# -----------------------------------------------------------
# STEP 4 — Infrastructure: DbSeeder
# -----------------------------------------------------------
Write-Step 'STEP 4 — Infrastructure: DbSeeder'

Write-File 'src\MojRok.Infrastructure\Persistence\DbSeeder.cs' @'
using Microsoft.EntityFrameworkCore;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence;

/// <summary>
/// Seeds reference data that must exist before the application serves traffic.
/// Called once at startup. Safe to call multiple times (idempotent).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedDefaultCategoriesAsync(AppDbContext db)
    {
        // Skip if defaults already exist
        if (await db.Categories.AnyAsync(c => c.IsDefault))
            return;

        var defaults = new Category[]
        {
            new() { Id = Guid.NewGuid(), Name = "Taxes",      Color = "#EF4444", IconSlug = "receipt-tax",  IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Documents",  Color = "#3B82F6", IconSlug = "document",     IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Health",     Color = "#10B981", IconSlug = "heart",        IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Vehicle",    Color = "#F59E0B", IconSlug = "truck",        IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Utilities",  Color = "#8B5CF6", IconSlug = "bolt",         IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Education",  Color = "#06B6D4", IconSlug = "academic-cap", IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Legal",      Color = "#64748B", IconSlug = "scale",        IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Other",      Color = "#6366F1", IconSlug = "tag",          IsDefault = true },
        };

        db.Categories.AddRange(defaults);
        await db.SaveChangesAsync();
    }
}
'@

# -----------------------------------------------------------
# STEP 5 — Patch Program.cs (targeted, preserves all existing config)
# -----------------------------------------------------------
Write-Step 'STEP 5 — Patching Program.cs'

$programPath = 'src\MojRok.API\Program.cs'
$prog = Get-Content $programPath -Raw
Write-Info "Read $programPath ($($prog.Length) chars)"

# 1 — using MojRok.Application.Categories
if ($prog -notmatch 'using MojRok\.Application\.Categories;') {
    $prog = $prog -replace 'using MojRok\.Application\.Users;',
        "using MojRok.Application.Categories;`r`nusing MojRok.Application.Users;"
    if ($prog -notmatch 'using MojRok\.Application\.Categories;') {
        Write-Fatal "Could not insert 'using MojRok.Application.Categories' into Program.cs"
    }
    Write-Done 'Added: using MojRok.Application.Categories'
} else { Write-Info 'Already present: using MojRok.Application.Categories' }

# 2 — using MojRok.Infrastructure.Persistence (needed for DbSeeder + AppDbContext)
if ($prog -notmatch 'using MojRok\.Infrastructure\.Persistence;') {
    $prog = $prog -replace 'using MojRok\.Infrastructure\.Extensions;',
        "using MojRok.Infrastructure.Extensions;`r`nusing MojRok.Infrastructure.Persistence;"
    if ($prog -notmatch 'using MojRok\.Infrastructure\.Persistence;') {
        Write-Fatal "Could not insert 'using MojRok.Infrastructure.Persistence' into Program.cs"
    }
    Write-Done 'Added: using MojRok.Infrastructure.Persistence'
} else { Write-Info 'Already present: using MojRok.Infrastructure.Persistence' }

# 3 — AddScoped<CategoryService>
if ($prog -notmatch 'AddScoped<CategoryService>') {
    $prog = $prog -replace 'AddScoped<UserService>\(\);',
        "AddScoped<UserService>();`r`nbuilder.Services.AddScoped<CategoryService>();"
    if ($prog -notmatch 'AddScoped<CategoryService>') {
        Write-Fatal "Could not insert 'AddScoped<CategoryService>' into Program.cs"
    }
    Write-Done 'Added: builder.Services.AddScoped<CategoryService>()'
} else { Write-Info 'Already present: AddScoped<CategoryService>' }

# 4 — Seeder call after app.Build()
if ($prog -notmatch 'DbSeeder\.SeedDefaultCategoriesAsync') {
    $prog = $prog -replace 'var app = builder\.Build\(\);',
        ("var app = builder.Build();`r`n`r`n" +
         "// Seed system default categories on first startup (idempotent)`r`n" +
         "using (var scope = app.Services.CreateScope())`r`n" +
         "{`r`n" +
         "    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();`r`n" +
         "    await DbSeeder.SeedDefaultCategoriesAsync(db);`r`n" +
         "}")
    if ($prog -notmatch 'DbSeeder\.SeedDefaultCategoriesAsync') {
        Write-Fatal "Could not insert DbSeeder call into Program.cs"
    }
    Write-Done 'Added: DbSeeder.SeedDefaultCategoriesAsync startup call'
} else { Write-Info 'Already present: DbSeeder call' }

Set-Content -Path $programPath -Value $prog -Encoding UTF8
Write-Done 'Program.cs saved'

# -----------------------------------------------------------
# STEP 6 — API: CategoriesController
# -----------------------------------------------------------
Write-Step 'STEP 6 — API: CategoriesController'

Write-File 'src\MojRok.API\Controllers\CategoriesController.cs' @'
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
    /// UserId is taken from JWT — never from the request body.
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
'@

# -----------------------------------------------------------
# STEP 7 — Tests: CategoryServiceTests
# -----------------------------------------------------------
Write-Step 'STEP 7 — Tests: CategoryServiceTests'

Write-File 'tests\MojRok.Tests\Categories\CategoryServiceTests.cs' @'
using Microsoft.EntityFrameworkCore;
using MojRok.Application.Categories;
using MojRok.Application.Categories.DTOs;
using MojRok.Application.Exceptions;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;
using MojRok.Infrastructure.Persistence;
using Xunit;

namespace MojRok.Tests.Categories;

/// <summary>
/// Unit tests for CategoryService using EF Core InMemory provider.
/// Each test gets a fresh isolated database to prevent state leakage.
/// </summary>
public class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext    _db;
    private readonly CategoryService _service;
    private readonly Guid            _userId    = Guid.NewGuid();
    private readonly Guid            _otherUser = Guid.NewGuid();

    public CategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db      = new AppDbContext(options);
        _service = new CategoryService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ---- Seed helpers -----------------------------------------------

    private async Task<Category> SeedDefaultAsync(string name = "Taxes")
    {
        var c = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = name,
            Color     = "#EF4444",
            IsDefault = true,
            UserId    = null
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }

    private async Task<Category> SeedUserCategoryAsync(
        Guid? userId = null,
        string name  = "My Category")
    {
        var c = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = name,
            Color     = "#3B82F6",
            IsDefault = false,
            UserId    = userId ?? _userId
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }

    private async Task SeedDeadlineForCategoryAsync(Guid categoryId)
    {
        _db.Deadlines.Add(new Deadline
        {
            Id         = Guid.NewGuid(),
            Title      = "Test Deadline",
            DueDate    = DateTime.UtcNow.AddDays(10),
            UserId     = _userId,
            CategoryId = categoryId,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // ================================================================
    // GET /api/categories
    // ================================================================

    [Fact]
    public async Task GetCategories_ReturnsDefaultCategories()
    {
        await SeedDefaultAsync("Taxes");
        await SeedDefaultAsync("Health");

        var result = await _service.GetCategoriesAsync(_userId);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsDefault));
    }

    [Fact]
    public async Task GetCategories_ReturnsCurrentUsersCategories()
    {
        await SeedDefaultAsync();
        await SeedUserCategoryAsync(_userId, "Personal");

        var result = await _service.GetCategoriesAsync(_userId);

        Assert.Contains(result, r => r.Name == "Personal");
    }

    [Fact]
    public async Task GetCategories_DoesNotReturnOtherUsersCategories()
    {
        await SeedUserCategoryAsync(_userId,    "Mine");
        await SeedUserCategoryAsync(_otherUser, "Theirs");

        var result = await _service.GetCategoriesAsync(_userId);

        Assert.Contains(result,    r => r.Name == "Mine");
        Assert.DoesNotContain(result, r => r.Name == "Theirs");
    }

    [Fact]
    public async Task GetCategories_DefaultsAppearBeforePersonal()
    {
        await SeedUserCategoryAsync(_userId, "ZZZ Personal");
        await SeedDefaultAsync("AAA Default");

        var result = await _service.GetCategoriesAsync(_userId);

        Assert.True(result[0].IsDefault);
        Assert.False(result[1].IsDefault);
    }

    // ================================================================
    // POST /api/categories
    // ================================================================

    [Fact]
    public async Task Create_SetsUserIdFromParameter()
    {
        var result = await _service.CreateCategoryAsync(_userId,
            new CreateCategoryRequest { Name = "Work", Color = "#111111" });

        Assert.Equal(_userId, result.UserId);
    }

    [Fact]
    public async Task Create_IsDefaultAlwaysFalse()
    {
        var result = await _service.CreateCategoryAsync(_userId,
            new CreateCategoryRequest { Name = "Work", Color = "#111111" });

        Assert.False(result.IsDefault);
    }

    [Fact]
    public async Task Create_TrimsName()
    {
        var result = await _service.CreateCategoryAsync(_userId,
            new CreateCategoryRequest { Name = "  Work  ", Color = "#111111" });

        Assert.Equal("Work", result.Name);
    }

    // ================================================================
    // PUT /api/categories/{id}
    // ================================================================

    [Fact]
    public async Task Update_UpdatesOwnCategory()
    {
        var cat = await SeedUserCategoryAsync(_userId);

        var result = await _service.UpdateCategoryAsync(_userId, cat.Id,
            new UpdateCategoryRequest { Name = "Updated", Color = "#FFFFFF" });

        Assert.Equal("Updated", result.Name);
        Assert.Equal("#FFFFFF", result.Color);
    }

    [Fact]
    public async Task Update_ThrowsForbidden_ForDefaultCategory()
    {
        var def = await SeedDefaultAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateCategoryAsync(_userId, def.Id,
                new UpdateCategoryRequest { Name = "Hack", Color = "#000000" }));
    }

    [Fact]
    public async Task Update_ThrowsForbidden_ForOtherUsersCategory()
    {
        var other = await SeedUserCategoryAsync(_otherUser);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateCategoryAsync(_userId, other.Id,
                new UpdateCategoryRequest { Name = "Hack", Color = "#000000" }));
    }

    [Fact]
    public async Task Update_ThrowsNotFound_WhenCategoryMissing()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateCategoryAsync(_userId, Guid.NewGuid(),
                new UpdateCategoryRequest { Name = "X", Color = "#000000" }));
    }

    // ================================================================
    // DELETE /api/categories/{id}
    // ================================================================

    [Fact]
    public async Task Delete_RemovesOwnCategory()
    {
        var cat = await SeedUserCategoryAsync(_userId);

        await _service.DeleteCategoryAsync(_userId, cat.Id);

        var exists = await _db.Categories.AnyAsync(c => c.Id == cat.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task Delete_ThrowsForbidden_ForDefaultCategory()
    {
        var def = await SeedDefaultAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.DeleteCategoryAsync(_userId, def.Id));
    }

    [Fact]
    public async Task Delete_ThrowsForbidden_ForOtherUsersCategory()
    {
        var other = await SeedUserCategoryAsync(_otherUser);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.DeleteCategoryAsync(_userId, other.Id));
    }

    [Fact]
    public async Task Delete_ThrowsConflict_WhenCategoryHasDeadlines()
    {
        var cat = await SeedUserCategoryAsync(_userId);
        await SeedDeadlineForCategoryAsync(cat.Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.DeleteCategoryAsync(_userId, cat.Id));
    }

    [Fact]
    public async Task Delete_ThrowsNotFound_WhenCategoryMissing()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteCategoryAsync(_userId, Guid.NewGuid()));
    }
}
'@

# -----------------------------------------------------------
# STEP 8 — restore, build, test
# -----------------------------------------------------------
Write-Step 'STEP 8 — dotnet restore'
dotnet restore
if ($LASTEXITCODE -ne 0) { Write-Fatal 'dotnet restore failed.' }
Write-Done 'Restore succeeded'

Write-Step 'STEP 8 — dotnet build --configuration Release'
dotnet build --no-restore --configuration Release
if ($LASTEXITCODE -ne 0) { Write-Fatal 'dotnet build failed.' }
Write-Done 'Build succeeded'

Write-Step 'STEP 8 — dotnet test --configuration Release'
dotnet test --no-build --configuration Release --verbosity normal
if ($LASTEXITCODE -ne 0) { Write-Fatal 'dotnet test failed.' }
Write-Done 'Tests succeeded'

# -----------------------------------------------------------
Write-Host ''
Write-Host '=== Phase 4 Complete ===' -ForegroundColor Yellow
Write-Host ''
Write-Host 'Swagger manual verification:' -ForegroundColor Cyan
Write-Host '  1. dotnet run --project src/MojRok.API'
Write-Host '  2. POST /api/Auth/login  -> copy token -> Authorize'
Write-Host '  3. GET  /api/Categories  -> 8 defaults + your personal categories'
Write-Host '  4. POST /api/Categories  -> create personal category -> 201'
Write-Host '  5. PUT  /api/Categories/{id}  -> update it -> 200'
Write-Host '  6. PUT  /api/Categories/{default-id}  -> 403 Forbidden'
Write-Host '  7. DELETE /api/Categories/{id} with deadlines -> 409 Conflict'
Write-Host '  8. DELETE /api/Categories/{id} without deadlines -> 204'
Write-Host ''
