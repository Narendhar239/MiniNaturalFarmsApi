using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Catalog;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Images;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Catalog;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly IImageService _images;

    public CategoryService(AppDbContext db, IImageService images)
    {
        _db = db;
        _images = images;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default) =>
        await _db.Categories.AsNoTracking()
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.NameEn)
            .Select(c => new CategoryDto(c.Id, c.NameEn, c.NameTe, c.ImageUrl, c.IsActive,
                c.Products.Count(p => p.IsActive)))
            .ToListAsync(ct);

    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto(c.Id, c.NameEn, c.NameTe, c.ImageUrl, c.IsActive,
                c.Products.Count(p => p.IsActive)))
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException($"Category {id} not found.");

    public async Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken ct = default)
    {
        var category = new Category
        {
            NameEn = request.NameEn.Trim(),
            NameTe = request.NameTe.Trim(),
            IsActive = request.IsActive
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(category.Id, ct);
    }

    public async Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct)
            ?? throw new NotFoundException($"Category {id} not found.");

        category.NameEn = request.NameEn.Trim();
        category.NameTe = request.NameTe.Trim();
        category.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<CategoryDto> SetImageAsync(int id, UploadFile file, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct)
            ?? throw new NotFoundException($"Category {id} not found.");

        var stored = await _images.UploadAsync(file.Content, file.FileName, "mininaturalfarms/categories", ct);
        var oldPublicId = category.ImagePublicId;

        category.ImageUrl = stored.Url;
        category.ImagePublicId = stored.PublicId;
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(oldPublicId))
            await _images.DeleteAsync(oldPublicId, CancellationToken.None);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct)
            ?? throw new NotFoundException($"Category {id} not found.");

        if (await _db.Products.AnyAsync(p => p.CategoryId == id, ct))
            throw new ConflictException("This category still has products. Move or deactivate them, or deactivate the category instead.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(category.ImagePublicId))
            await _images.DeleteAsync(category.ImagePublicId, CancellationToken.None);
    }
}