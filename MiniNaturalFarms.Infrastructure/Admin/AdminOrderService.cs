using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Orders;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Domain.Enums;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Admin;

public class AdminOrderService : IAdminOrderService
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly ILogger<AdminOrderService> _logger;

    public AdminOrderService(AppDbContext db, IPaymentGateway gateway, ILogger<AdminOrderService> logger)
    {
        _db = db;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<PagedResult<AdminOrderSummaryDto>> GetPagedAsync(AdminOrderQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.Orders.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
            q = q.Where(o => o.Status == query.Status.Value);
        if (query.PaymentStatus.HasValue)
            q = q.Where(o => o.PaymentStatus == query.PaymentStatus.Value);
        if (query.PaymentMethod.HasValue)
            q = q.Where(o => o.PaymentMethod == query.PaymentMethod.Value);

        if (query.From.HasValue)
        {
            var from = IstTime.StartUtc(query.From.Value);
            q = q.Where(o => o.CreatedAt >= from);
        }
        if (query.To.HasValue)
        {
            var to = IstTime.EndUtcExclusive(query.To.Value);
            q = q.Where(o => o.CreatedAt < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s)
                             || o.ShipFullName.Contains(s)
                             || o.ShipPhone.Contains(s)
                             || _db.Users.Any(u => u.Id == o.UserId && u.Email != null && u.Email.Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var pageQuery = q.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize);

        return new PagedResult<AdminOrderSummaryDto>
        {
            Items = await AdminOrderMapping.ToSummariesAsync(pageQuery, _db, ct),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AdminOrderDto> GetAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Order not found.");

        var email = await _db.Users.AsNoTracking()
            .Where(u => u.Id == order.UserId).Select(u => u.Email).FirstOrDefaultAsync(ct) ?? string.Empty;

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var images = await _db.ProductImages.AsNoTracking()
            .Where(i => productIds.Contains(i.ProductId))
            .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
            .Select(i => new { i.ProductId, i.Url })
            .ToListAsync(ct);
        var imageMap = images.GroupBy(i => i.ProductId).ToDictionary(g => g.Key, g => g.First().Url);

        var items = order.Items.OrderBy(i => i.Id)
            .Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, imageMap.GetValueOrDefault(i.ProductId),
                i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))
            .ToList();

        return new AdminOrderDto(
            order.Id, order.OrderNumber, AdminOrderMapping.AsUtc(order.CreatedAt),
            order.Status.ToString(), order.PaymentMethod.ToString(), order.PaymentStatus.ToString(),
            order.SubTotal, order.ShippingCharge, order.TotalAmount,
            email,
            order.ShipFullName, order.ShipPhone, order.ShipAddress,
            order.ShipCity, order.ShipState, order.ShipPincode,
            order.RazorpayOrderId, order.RazorpayPaymentId, order.RazorpayRefundId,
            order.RefundedAt.HasValue ? AdminOrderMapping.AsUtc(order.RefundedAt.Value) : null,
            items,
            AllowedNext(order).Select(s => s.ToString()).ToList());
    }

    public async Task<AdminOrderDto> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Order not found.");

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var target) || !Enum.IsDefined(target))
            throw new BadRequestException("Unknown status.");

        if (!AllowedNext(order).Contains(target))
            throw new ConflictException($"An order that is {order.Status} can't be moved to {target}.");

        if (target == OrderStatus.Cancelled)
        {
            await RestockAndCancelAsync(order, ct);
        }
        else
        {
            order.Status = target;
            if (target == OrderStatus.Delivered && order.PaymentMethod == PaymentMethod.Cod)
                order.PaymentStatus = PaymentStatus.Paid; // cash collected on delivery

            await _db.SaveChangesAsync(ct);
        }

        return await GetAsync(id, ct);
    }

    public async Task<AdminOrderDto> RefundAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Order not found.");

        if (order.PaymentMethod == PaymentMethod.Cod)
            throw new BadRequestException("Cash on Delivery orders have no online payment to refund.");

        if (order.PaymentStatus == PaymentStatus.Refunded || !string.IsNullOrEmpty(order.RazorpayRefundId))
            throw new ConflictException("This order has already been refunded.");

        if (order.PaymentStatus != PaymentStatus.Paid || string.IsNullOrEmpty(order.RazorpayPaymentId))
            throw new ConflictException("This order has no captured payment to refund.");

        if (order.Status != OrderStatus.Cancelled)
            throw new ConflictException("Cancel the order first, then refund it.");

        var refundId = await _gateway.RefundAsync(
            order.RazorpayPaymentId, ToPaise(order.TotalAmount), order.OrderNumber, ct);

        try
        {
            order.RazorpayRefundId = refundId;
            order.PaymentStatus = PaymentStatus.Refunded;
            order.RefundedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "Refund {RefundId} was created at Razorpay for order {OrderNumber} but saving it failed.",
                refundId, order.OrderNumber);
            throw;
        }

        return await GetAsync(id, ct);
    }

    private static IReadOnlyList<OrderStatus> AllowedNext(Order o)
    {
        var cod = o.PaymentMethod == PaymentMethod.Cod;
        return o.Status switch
        {
            OrderStatus.Pending => cod
                ? [OrderStatus.Confirmed, OrderStatus.Cancelled]
                : [OrderStatus.Cancelled],
            OrderStatus.Confirmed => [OrderStatus.Processing, OrderStatus.Cancelled],
            OrderStatus.Processing => [OrderStatus.Dispatched, OrderStatus.Cancelled],
            OrderStatus.Dispatched => [OrderStatus.Delivered],
            _ => []
        };
    }

    private async Task RestockAndCancelAsync(Order order, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var item in order.Items)
        {
            var productId = item.ProductId;
            var qty = item.Quantity;
            await _db.Products.Where(p => p.Id == productId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + qty), ct);
        }

        order.Status = OrderStatus.Cancelled;
        if (order.PaymentStatus == PaymentStatus.Pending)
            order.PaymentStatus = PaymentStatus.Failed;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static long ToPaise(decimal rupees) =>
        (long)Math.Round(rupees * 100m, MidpointRounding.AwayFromZero);
}