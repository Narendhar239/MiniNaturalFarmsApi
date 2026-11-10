using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Catalog;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Images;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Catalog;

public class ProductService : IProductService
{
    private const int MaxImagesPerProduct = 8;
    private const int MaxPageSize = 50;

    private readonly AppDbContext _db;
    private readonly IImageService _images;

    public ProductService(AppDbContext db, IImageService images)
    {
        _db = db;
        _images = images;
    }

    private static IQueryable<ProductDto> Project(IQueryable<Product> q) =>
        q.Select(p => new ProductDto(
            p.Id, p.NameEn, p.NameTe, p.DescriptionEn, p.DescriptionTe,
            p.Price, p.DiscountPrice, p.StockQuantity, p.IsActive,
            p.CategoryId, p.Category.NameEn, p.Category.NameTe,
            p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                .Select(i => new ProductImageDto(i.Id, i.Url, i.IsPrimary)).ToList()));

    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.Products.AsNoTracking().AsQueryable();

        if (!query.IncludeInactive)
            q = q.Where(p => p.IsActive && p.Category.IsActive);

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(p => p.NameEn.Contains(s) || p.NameTe.Contains(s));
        }

        // Price filters apply to the price the customer actually pays
        if (query.MinPrice.HasValue)
            q = q.Where(p => (p.DiscountPrice ?? p.Price) >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue)
            q = q.Where(p => (p.DiscountPrice ?? p.Price) <= query.MaxPrice.Value);

        q = query.Sort?.ToLowerInvariant() switch
        {
            "price_asc" => q.OrderBy(p => p.DiscountPrice ?? p.Price).ThenBy(p => p.Id),
            "price_desc" => q.OrderByDescending(p => p.DiscountPrice ?? p.Price).ThenBy(p => p.Id),
            "name" => q.OrderBy(p => p.NameEn).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
        };

        var total = await q.CountAsync(ct);
        var items = await Project(q.Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);

        return new PagedResult<ProductDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ProductDto> GetByIdAsync(int id, bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.Products.AsNoTracking().Where(p => p.Id == id);
        if (!includeInactive)
            q = q.Where(p => p.IsActive && p.Category.IsActive);

        return await Project(q).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Product {id} not found.");
    }

    public async Task<ProductDto> CreateAsync(SaveProductRequest request, CancellationToken ct = default)
    {
        await EnsureCategoryExists(request.CategoryId, ct);

        var product = new Product();
        Apply(product, request);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(product.Id, true, ct);
    }

    public async Task<ProductDto> UpdateAsync(int id, SaveProductRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException($"Product {id} not found.");

        await EnsureCategoryExists(request.CategoryId, ct);
        Apply(product, request);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, true, ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException($"Product {id} not found.");
        product.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProductDto> AddImagesAsync(int productId, IReadOnlyList<UploadFile> files, CancellationToken ct = default)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new NotFoundException($"Product {productId} not found.");

        if (product.Images.Count + files.Count > MaxImagesPerProduct)
            throw new ConflictException($"A product can have at most {MaxImagesPerProduct} images.");

        var uploaded = new List<StoredImage>();
        try
        {
            foreach (var file in files)
                uploaded.Add(await _images.UploadAsync(file.Content, file.FileName, $"mininaturalfarms/products/{productId}", ct));

            var hasPrimary = product.Images.Any(i => i.IsPrimary);
            foreach (var s in uploaded)
            {
                product.Images.Add(new ProductImage { Url = s.Url, PublicId = s.PublicId, IsPrimary = !hasPrimary });
                hasPrimary = true; // only the very first image becomes primary
            }
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Don't leave orphaned files in Cloudinary if anything failed
            foreach (var s in uploaded)
                await _images.DeleteAsync(s.PublicId, CancellationToken.None);
            throw;
        }

        return await GetByIdAsync(productId, true, ct);
    }

    public async Task<ProductDto> DeleteImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        var image = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct)
            ?? throw new NotFoundException("Image not found for this product.");

        _db.ProductImages.Remove(image);

        if (image.IsPrimary)
        {
            var next = await _db.ProductImages
                .Where(i => i.ProductId == productId && i.Id != imageId)
                .OrderBy(i => i.Id).FirstOrDefaultAsync(ct);
            if (next != null) next.IsPrimary = true;
        }

        await _db.SaveChangesAsync(ct);
        await _images.DeleteAsync(image.PublicId, CancellationToken.None);
        return await GetByIdAsync(productId, true, ct);
    }

    public async Task<ProductDto> SetPrimaryImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        var images = await _db.ProductImages.Where(i => i.ProductId == productId).ToListAsync(ct);
        if (images.All(i => i.Id != imageId))
            throw new NotFoundException("Image not found for this product.");

        foreach (var i in images) i.IsPrimary = i.Id == imageId;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(productId, true, ct);
    }

    private async Task EnsureCategoryExists(int categoryId, CancellationToken ct)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            throw new NotFoundException($"Category {categoryId} not found.");
    }

    private static void Apply(Product p, SaveProductRequest r)
    {
        p.NameEn = r.NameEn.Trim();
        p.NameTe = r.NameTe.Trim();
        p.DescriptionEn = r.DescriptionEn.Trim();
        p.DescriptionTe = r.DescriptionTe.Trim();
        p.Price = r.Price;
        p.DiscountPrice = r.DiscountPrice;
        p.StockQuantity = r.StockQuantity;
        p.CategoryId = r.CategoryId;
        p.IsActive = r.IsActive;
    }
}