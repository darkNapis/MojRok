using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Application.Exceptions;
using MojRok.Application.Services.DTOs;
using MojRok.Domain.Entities;

namespace MojRok.Application.Services;

public class CitizenServiceService
{
    private readonly IAppDbContext _db;

    public CitizenServiceService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CitizenServiceResponse>> GetAllAsync(
        int? categoryId = null,
        int? municipalityId = null,
        CancellationToken ct = default)
    {
        var query = _db.CitizenServices
            .AsNoTracking()
            .Include(s => s.ServiceCategory)
            .Include(s => s.Municipality)
            .Where(s => s.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(s => s.ServiceCategoryId == categoryId.Value);

        if (municipalityId.HasValue)
            query = query.Where(s => s.MunicipalityId == municipalityId.Value);

        var list = await query
            .OrderBy(s => s.ServiceCategory.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<CitizenServiceResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var service = await _db.CitizenServices
            .AsNoTracking()
            .Include(s => s.ServiceCategory)
            .Include(s => s.Municipality)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (service is null)
            throw new NotFoundException($"Service {id} was not found.");

        return ToResponse(service);
    }

    public async Task<List<ServiceCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _db.ServiceCategories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

        return categories.Select(c => new ServiceCategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            NameMk = c.NameMk,
            IconSlug = c.IconSlug,
            SortOrder = c.SortOrder
        }).ToList();
    }

    public async Task<CitizenServiceResponse> CreateAsync(
        CreateCitizenServiceRequest request,
        CancellationToken ct = default)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == request.ServiceCategoryId, ct);

        if (category is null)
            throw new NotFoundException("Service category not found.");

        if (request.MunicipalityId.HasValue)
        {
            var exists = await _db.Municipalities
                .AnyAsync(m => m.Id == request.MunicipalityId.Value, ct);
            if (!exists)
                throw new NotFoundException("Municipality not found.");
        }

        var entity = new CitizenService
        {
            Name = request.Name.Trim(),
            NameMk = request.NameMk.Trim(),
            Description = request.Description?.Trim(),
            DescriptionMk = request.DescriptionMk?.Trim(),
            ServiceCategoryId = request.ServiceCategoryId,
            MunicipalityId = request.MunicipalityId,
            WebsiteUrl = request.WebsiteUrl.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = request.IsActive
        };

        _db.CitizenServices.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Reload with navigations
        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<CitizenServiceResponse> UpdateAsync(
        int id,
        UpdateCitizenServiceRequest request,
        CancellationToken ct = default)
    {
        var entity = await _db.CitizenServices
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (entity is null)
            throw new NotFoundException($"Service {id} was not found.");

        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == request.ServiceCategoryId, ct);

        if (category is null)
            throw new NotFoundException("Service category not found.");

        if (request.MunicipalityId.HasValue)
        {
            var exists = await _db.Municipalities
                .AnyAsync(m => m.Id == request.MunicipalityId.Value, ct);
            if (!exists)
                throw new NotFoundException("Municipality not found.");
        }

        entity.Name = request.Name.Trim();
        entity.NameMk = request.NameMk.Trim();
        entity.Description = request.Description?.Trim();
        entity.DescriptionMk = request.DescriptionMk?.Trim();
        entity.ServiceCategoryId = request.ServiceCategoryId;
        entity.MunicipalityId = request.MunicipalityId;
        entity.WebsiteUrl = request.WebsiteUrl.Trim();
        entity.PhoneNumber = request.PhoneNumber?.Trim();
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.CitizenServices
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (entity is null)
            throw new NotFoundException($"Service {id} was not found.");

        _db.CitizenServices.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private static CitizenServiceResponse ToResponse(CitizenService s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        NameMk = s.NameMk,
        Description = s.Description,
        DescriptionMk = s.DescriptionMk,
        ServiceCategoryId = s.ServiceCategoryId,
        CategoryName = s.ServiceCategory?.Name ?? string.Empty,
        CategoryNameMk = s.ServiceCategory?.NameMk ?? string.Empty,
        MunicipalityId = s.MunicipalityId,
        MunicipalityName = s.Municipality?.Name,
        MunicipalityNameMk = s.Municipality?.NameMk,
        WebsiteUrl = s.WebsiteUrl,
        PhoneNumber = s.PhoneNumber,
        IsActive = s.IsActive
    };
}