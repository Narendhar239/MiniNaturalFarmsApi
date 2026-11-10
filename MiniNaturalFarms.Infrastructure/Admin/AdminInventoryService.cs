using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Admin;

public class AdminInventoryService : IAdminInventoryService
{
    private const int MaxPageSize = 100;
    private readonly AppDbContext _db;

    public AdminInventoryService(AppDbContext db) => _db = db;

    internal static IQueryable<InventoryItemDto> Project(IQueryable<Product> q, int threshold) =>
        q.Select(p => new InventoryItemDto(
            p.Id, p.NameEn, p.NameTe, p.Category.NameEn,
            p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id).Select(i => i.Url).FirstOrDefault(),
            p.Price, p.DiscountPrice, p.StockQuantity, p.IsActive,
            p.StockQuantity == 0 ? "OutOfStock" : (p.StockQuantity <= threshold ? "Low" : "InStock")));

    public async Task<PagedResult<InventoryItemDto>> GetAsync(InventoryQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var threshold = Math.Clamp(query.LowStockThreshold, 1, 1000);

        var q = _db.Products.AsNoTracking().AsQueryable();

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(p => p.NameEn.Contains(s) || p.NameTe.Contains(s));
        }

        q = query.Filter switch
        {
            StockFilter.Low => q.Where(p => p.StockQuantity > 0 && p.StockQuantity <= threshold),
            StockFilter.Out => q.Where(p => p.StockQuantity == 0),
            _ => q
        };

        var total = await q.CountAsync(ct);
        var items = await Project(
                q.OrderBy(p => p.StockQuantity).ThenBy(p => p.NameEn).ThenBy(p => p.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize),
                threshold)
            .ToListAsync(ct);

        return new PagedResult<InventoryItemDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<InventoryItemDto> SetStockAsync(int productId, int quantity, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync([productId], ct)
            ?? throw new NotFoundException($"Product {productId} not found.");

        product.StockQuantity = quantity;
        await _db.SaveChangesAsync(ct);

        return await Project(
                _db.Products.AsNoTracking().Where(p => p.Id == productId),
                InventoryRules.DefaultLowStockThreshold)
            .FirstAsync(ct);
    }
}