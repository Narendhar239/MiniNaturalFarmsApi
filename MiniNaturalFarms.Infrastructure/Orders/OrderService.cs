using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Orders;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Domain.Enums;
using MiniNaturalFarms.Infrastructure.Persistence;
using MiniNaturalFarms.Infrastructure.Settings;

namespace MiniNaturalFarms.Infrastructure.Orders;

public class OrderService : IOrderService
{
    private const int MaxPageSize = 50;

    private readonly AppDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly ShippingSettings _shipping;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, IPaymentGateway gateway,
        IOptions<ShippingSettings> shipping, ILogger<OrderService> logger)
    {
        _db = db;
        _gateway = gateway;
        _shipping = shipping.Value;
        _logger = logger;
    }

    // ---------------------------------------------------------------- checkout

    public async Task<CheckoutResultDto> CheckoutAsync(string userId, CheckoutRequest request, CancellationToken ct = default)
    {
        var cart = await _db.CartItems
            .Include(c => c.Product).ThenInclude(p => p.Category)
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

        if (cart.Count == 0)
            throw new BadRequestException("Your cart is empty.");

        var problems = new List<string>();
        foreach (var c in cart)
        {
            var p = c.Product;
            if (!p.IsActive || !p.Category.IsActive)
                problems.Add($"'{p.NameEn}' is no longer available.");
            else if (p.StockQuantity < c.Quantity)
                problems.Add(p.StockQuantity == 0
                    ? $"'{p.NameEn}' is out of stock."
                    : $"Only {p.StockQuantity} of '{p.NameEn}' left.");
        }
        if (problems.Count > 0)
            throw new ConflictException(string.Join(" ", problems));

        var address = await _db.Addresses.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, ct)
            ?? throw new NotFoundException("Address not found.");

        var subTotal = cart.Sum(c => (c.Product.DiscountPrice ?? c.Product.Price) * c.Quantity);
        var shipping = subTotal >= _shipping.FreeShippingThreshold ? 0m : _shipping.FlatRate;
        var total = subTotal + shipping;

        var method = request.PaymentMethod == CheckoutPaymentMethod.Online ? PaymentMethod.Online : PaymentMethod.Cod;
        var orderNumber = GenerateOrderNumber();

        // Talk to Razorpay BEFORE touching the database, so a gateway failure leaves nothing behind
        string? razorpayOrderId = null;
        if (method == PaymentMethod.Online)
            razorpayOrderId = await _gateway.CreateOrderAsync(ToPaise(total), orderNumber, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Reserve stock atomically: the UPDATE only succeeds if enough stock is still there
        foreach (var c in cart)
        {
            var productId = c.ProductId;
            var qty = c.Quantity;
            var rows = await _db.Products
                .Where(p => p.Id == productId && p.StockQuantity >= qty)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - qty), ct);

            if (rows == 0)
                throw new ConflictException($"'{c.Product.NameEn}' just went out of stock. Please review your cart.");
        }

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            SubTotal = subTotal,
            ShippingCharge = shipping,
            TotalAmount = total,
            Status = OrderStatus.Pending,
            PaymentMethod = method,
            PaymentStatus = PaymentStatus.Pending,
            RazorpayOrderId = razorpayOrderId,
            ShipFullName = address.FullName,
            ShipPhone = address.Phone,
            ShipAddress = string.IsNullOrWhiteSpace(address.Line2) ? address.Line1 : $"{address.Line1}, {address.Line2}",
            ShipCity = address.City,
            ShipState = address.State,
            ShipPincode = address.Pincode
        };

        foreach (var c in cart)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = c.ProductId,
                ProductName = c.Product.NameEn,
                UnitPrice = c.Product.DiscountPrice ?? c.Product.Price,
                Quantity = c.Quantity
            });
        }

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cart);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var dto = await LoadDtoAsync(order.Id, userId, ct);
        RazorpayPaymentDto? payment = razorpayOrderId == null
            ? null
            : new RazorpayPaymentDto(_gateway.KeyId, razorpayOrderId, ToPaise(total), "INR", orderNumber);

        return new CheckoutResultDto(dto, payment);
    }

    // ---------------------------------------------------------------- queries

    public async Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var q = _db.Orders.AsNoTracking().Where(o => o.UserId == userId);
        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.CreatedAt, o.Status, o.PaymentMethod,
                o.PaymentStatus, o.TotalAmount,
                ItemCount = o.Items.Sum(i => i.Quantity)
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new OrderSummaryDto(
            r.Id, r.OrderNumber, AsUtc(r.CreatedAt),
            r.Status.ToString(), r.PaymentMethod.ToString(), r.PaymentStatus.ToString(),
            r.TotalAmount, r.ItemCount)).ToList();

        return new PagedResult<OrderSummaryDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<OrderDto> GetMyOrderAsync(string userId, int orderId, CancellationToken ct = default) =>
        LoadDtoAsync(orderId, userId, ct);

    // ---------------------------------------------------------------- cancel

    public async Task<OrderDto> CancelAsync(string userId, int orderId, CancellationToken ct = default)
    {
        var order = await FindOwnedAsync(userId, orderId, ct);

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new ConflictException("This order can no longer be cancelled.");

        if (order.PaymentMethod == PaymentMethod.Online && order.PaymentStatus == PaymentStatus.Paid)
            throw new ConflictException("Paid online orders can't be cancelled here. Please contact support for a refund.");

        await RestockAndCancelAsync(order, ct);
        return await LoadDtoAsync(orderId, userId, ct);
    }

    public async Task<int> CancelExpiredUnpaidOrdersAsync(TimeSpan maxAge, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;

        var expired = await _db.Orders.Include(o => o.Items)
            .Where(o => o.PaymentMethod == PaymentMethod.Online
                        && o.PaymentStatus == PaymentStatus.Pending
                        && o.Status == OrderStatus.Pending
                        && o.CreatedAt < cutoff)
            .ToListAsync(ct);

        foreach (var order in expired)
            await RestockAndCancelAsync(order, ct);

        return expired.Count;
    }

    // ---------------------------------------------------------------- payment

    public async Task<RazorpayPaymentDto> GetPaymentAsync(string userId, int orderId, CancellationToken ct = default)
    {
        var order = await FindOwnedAsync(userId, orderId, ct);

        if (order.PaymentMethod != PaymentMethod.Online || order.PaymentStatus != PaymentStatus.Pending
            || order.Status != OrderStatus.Pending)
            throw new ConflictException("This order doesn't need a payment.");

        if (string.IsNullOrEmpty(order.RazorpayOrderId))
        {
            order.RazorpayOrderId = await _gateway.CreateOrderAsync(ToPaise(order.TotalAmount), order.OrderNumber, ct);
            await _db.SaveChangesAsync(ct);
        }

        return new RazorpayPaymentDto(_gateway.KeyId, order.RazorpayOrderId, ToPaise(order.TotalAmount), "INR", order.OrderNumber);
    }

    public async Task<OrderDto> VerifyPaymentAsync(string userId, int orderId, VerifyPaymentRequest request, CancellationToken ct = default)
    {
        var order = await FindOwnedAsync(userId, orderId, ct);

        if (order.PaymentMethod != PaymentMethod.Online)
            throw new BadRequestException("This is not an online payment order.");

        // The webhook may already have confirmed it
        if (order.PaymentStatus == PaymentStatus.Paid)
            return await LoadDtoAsync(orderId, userId, ct);

        if (order.Status == OrderStatus.Cancelled)
            throw new ConflictException("This order was cancelled. If money was debited it will be refunded.");

        if (order.RazorpayOrderId != request.RazorpayOrderId)
            throw new BadRequestException("This payment does not belong to this order.");

        if (!_gateway.VerifyPaymentSignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature))
            throw new BadRequestException("Payment verification failed.");

        await MarkPaidAsync(order, request.RazorpayPaymentId, ct);
        return await LoadDtoAsync(orderId, userId, ct);
    }

    public async Task MarkPaidByGatewayOrderAsync(string razorpayOrderId, string razorpayPaymentId, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.RazorpayOrderId == razorpayOrderId, ct);
        if (order == null)
        {
            _logger.LogWarning("Webhook for unknown Razorpay order {RazorpayOrderId}", razorpayOrderId);
            return;
        }

        if (order.PaymentStatus == PaymentStatus.Paid) return;
        await MarkPaidAsync(order, razorpayPaymentId, ct);
    }

    // ---------------------------------------------------------------- helpers

    private async Task MarkPaidAsync(Order order, string paymentId, CancellationToken ct)
    {
        order.RazorpayPaymentId = paymentId;
        order.PaymentStatus = PaymentStatus.Paid;

        if (order.Status == OrderStatus.Pending)
            order.Status = OrderStatus.Confirmed;
        else if (order.Status == OrderStatus.Cancelled)
            _logger.LogWarning("Payment {PaymentId} arrived for CANCELLED order {OrderNumber}. A refund is needed.",
                paymentId, order.OrderNumber);

        await _db.SaveChangesAsync(ct);
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
        if (order.PaymentMethod == PaymentMethod.Online && order.PaymentStatus == PaymentStatus.Pending)
            order.PaymentStatus = PaymentStatus.Failed;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task<Order> FindOwnedAsync(string userId, int orderId, CancellationToken ct) =>
        await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
        ?? throw new NotFoundException("Order not found.");

    private async Task<OrderDto> LoadDtoAsync(int orderId, string userId, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
            ?? throw new NotFoundException("Order not found.");

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var images = await _db.ProductImages.AsNoTracking()
            .Where(i => productIds.Contains(i.ProductId))
            .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
            .Select(i => new { i.ProductId, i.Url })
            .ToListAsync(ct);

        var imageMap = images.GroupBy(i => i.ProductId).ToDictionary(g => g.Key, g => g.First().Url);
        return ToDto(order, imageMap);
    }

    private static OrderDto ToDto(Order o, Dictionary<int, string> images) => new(
        o.Id, o.OrderNumber, AsUtc(o.CreatedAt),
        o.Status.ToString(), o.PaymentMethod.ToString(), o.PaymentStatus.ToString(),
        o.SubTotal, o.ShippingCharge, o.TotalAmount,
        o.ShipFullName, o.ShipPhone, o.ShipAddress, o.ShipCity, o.ShipState, o.ShipPincode,
        o.Items.OrderBy(i => i.Id)
            .Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, images.GetValueOrDefault(i.ProductId),
                i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))
            .ToList());

    // SQL Server returns DateTime with Kind=Unspecified; mark it UTC so JSON gets a "Z"
    private static DateTime AsUtc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    private static long ToPaise(decimal rupees) =>
        (long)Math.Round(rupees * 100m, MidpointRounding.AwayFromZero);

    private static string GenerateOrderNumber()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I
        return $"MNF-{DateTime.UtcNow:yyMMdd}-{RandomNumberGenerator.GetString(alphabet, 6)}";
    }
}