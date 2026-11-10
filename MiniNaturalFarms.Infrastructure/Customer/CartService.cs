using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Customer;

public class CartService : ICartService
{
    private const int MaxQtyPerItem = 99;
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public async Task<CartDto> GetAsync(string userId, CancellationToken ct = default)
    {
        var items = await _db.CartItems.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .Select(c => new CartItemDto(
                c.Id,
                c.ProductId,
                c.Product.NameEn,
                c.Product.NameTe,
                c.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                    .Select(i => i.Url).FirstOrDefault(),
                c.Product.Price,
                c.Product.DiscountPrice,
                c.Product.DiscountPrice ?? c.Product.Price,
                c.Quantity,
                (c.Product.DiscountPrice ?? c.Product.Price) * c.Quantity,
                c.Product.StockQuantity,
                c.Product.IsActive && c.Product.Category.IsActive && c.Product.StockQuantity >= c.Quantity))
            .ToListAsync(ct);

        return new CartDto(
            items,
            items.Sum(i => i.Quantity),
            items.Where(i => i.IsAvailable).Sum(i => i.LineTotal),
            items.Any(i => !i.IsAvailable));
    }

    public async Task<CartDto> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken ct = default)
    {
        var stock = await GetAvailableStockAsync(request.ProductId, ct);

        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId, ct);

        var newQty = (item?.Quantity ?? 0) + request.Quantity;
        EnsureQuantityAllowed(newQty, stock);

        if (item == null)
            _db.CartItems.Add(new CartItem { UserId = userId, ProductId = request.ProductId, Quantity = newQty });
        else
            item.Quantity = newQty;

        await _db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<CartDto> SetQuantityAsync(string userId, int productId, int quantity, CancellationToken ct = default)
    {
        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId, ct)
            ?? throw new NotFoundException("Item is not in your cart.");

        var stock = await GetAvailableStockAsync(productId, ct);
        EnsureQuantityAllowed(quantity, stock);

        item.Quantity = quantity;
        await _db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<CartDto> RemoveItemAsync(string userId, int productId, CancellationToken ct = default)
    {
        await _db.CartItems
            .Where(c => c.UserId == userId && c.ProductId == productId)
            .ExecuteDeleteAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task ClearAsync(string userId, CancellationToken ct = default) =>
        await _db.CartItems.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct);

    private async Task<int> GetAvailableStockAsync(int productId, CancellationToken ct) =>
        await _db.Products
            .Where(p => p.Id == productId && p.IsActive && p.Category.IsActive)
            .Select(p => (int?)p.StockQuantity)
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("This product is not available.");

    private static void EnsureQuantityAllowed(int quantity, int stock)
    {
        if (stock <= 0)
            throw new ConflictException("This product is out of stock.");
        if (quantity > MaxQtyPerItem)
            throw new ConflictException($"You can add at most {MaxQtyPerItem} of one item.");
        if (quantity > stock)
            throw new ConflictException($"Only {stock} left in stock.");
    }
}