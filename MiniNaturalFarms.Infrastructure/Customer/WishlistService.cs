using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Customer;

public class WishlistService : IWishlistService
{
    private readonly AppDbContext _db;

    public WishlistService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WishlistItemDto>> GetAsync(string userId, CancellationToken ct = default) =>
        await _db.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WishlistItemDto(
                w.ProductId,
                w.Product.NameEn,
                w.Product.NameTe,
                w.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                    .Select(i => i.Url).FirstOrDefault(),
                w.Product.Price,
                w.Product.DiscountPrice,
                w.Product.IsActive && w.Product.Category.IsActive && w.Product.StockQuantity > 0,
                w.CreatedAt))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<int>> GetProductIdsAsync(string userId, CancellationToken ct = default) =>
        await _db.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => w.ProductId)
            .ToListAsync(ct);

    public async Task AddAsync(string userId, int productId, CancellationToken ct = default)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive && p.Category.IsActive, ct);
        if (!exists) throw new NotFoundException("This product is not available.");

        // Adding twice is fine (idempotent)
        if (await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId, ct))
            return;

        _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string userId, int productId, CancellationToken ct = default) =>
        await _db.WishlistItems
            .Where(w => w.UserId == userId && w.ProductId == productId)
            .ExecuteDeleteAsync(ct);
}