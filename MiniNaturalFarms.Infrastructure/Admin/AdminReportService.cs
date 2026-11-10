using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Domain.Constants;
using MiniNaturalFarms.Domain.Enums;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Admin;

internal sealed record SalesOrderRow(DateTime CreatedAt, decimal TotalAmount, PaymentStatus PaymentStatus, int Units);

public class AdminReportService : IAdminReportService
{
    private readonly AppDbContext _db;

    public AdminReportService(AppDbContext db) => _db = db;

    public async Task<SalesReportDto> GetSalesAsync(DateOnly? from, DateOnly? to, string? groupBy, CancellationToken ct = default)
    {
        var (f, t) = IstTime.ResolveRange(from, to);
        var monthly = string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase);
        var startUtc = IstTime.StartUtc(f);
        var endUtc = IstTime.EndUtcExclusive(t);

        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= startUtc && o.CreatedAt < endUtc && o.Status != OrderStatus.Cancelled)
            .Select(o => new SalesOrderRow(o.CreatedAt, o.TotalAmount, o.PaymentStatus, o.Items.Sum(i => i.Quantity)))
            .ToListAsync(ct);

        string Key(DateOnly d) => d.ToString(monthly ? "yyyy-MM" : "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var grouped = orders
            .GroupBy(o => Key(IstTime.ToIstDate(o.CreatedAt)))
            .ToDictionary(g => g.Key, g => g.ToList());

        var keys = new List<string>();
        for (var d = f; d <= t; d = d.AddDays(1))
        {
            var k = Key(d);
            if (keys.Count == 0 || keys[^1] != k) keys.Add(k);
        }

        var rows = new List<SalesPointDto>();
        foreach (var k in keys)
        {
            var list = grouped.GetValueOrDefault(k) ?? [];
            var paid = list.Where(x => x.PaymentStatus == PaymentStatus.Paid).ToList();
            var revenue = paid.Sum(x => x.TotalAmount);
            var aov = paid.Count > 0 ? Math.Round(revenue / paid.Count, 2) : 0m;

            rows.Add(new SalesPointDto(k, list.Count, list.Sum(x => x.Units), revenue, aov));
        }

        return new SalesReportDto(
            f, t, monthly ? "month" : "day",
            rows.Sum(r => r.Orders), rows.Sum(r => r.Units), rows.Sum(r => r.Revenue), rows);
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(DateOnly? from, DateOnly? to, int take, CancellationToken ct = default)
    {
        var (f, t) = IstTime.ResolveRange(from, to);
        var startUtc = IstTime.StartUtc(f);
        var endUtc = IstTime.EndUtcExclusive(t);
        take = Math.Clamp(take, 1, 50);

        var rows = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.CreatedAt >= startUtc && i.Order.CreatedAt < endUtc && i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Name = g.Max(i => i.ProductName),
                Units = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.UnitPrice * i.Quantity)
            })
            .OrderByDescending(x => x.Units).ThenByDescending(x => x.Revenue)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(r => new TopProductDto(r.ProductId, r.Name, r.Units, r.Revenue)).ToList();
    }

    public async Task<InventoryReportDto> GetInventoryAsync(int lowStockThreshold, CancellationToken ct = default)
    {
        var threshold = Math.Clamp(lowStockThreshold, 1, 1000);
        var active = _db.Products.AsNoTracking().Where(p => p.IsActive);

        var total = await _db.Products.CountAsync(ct);
        var activeCount = await active.CountAsync(ct);
        var outOfStock = await active.CountAsync(p => p.StockQuantity == 0, ct);
        var low = await active.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= threshold, ct);
        var stockValue = await active.SumAsync(p => (decimal?)(p.StockQuantity * (p.DiscountPrice ?? p.Price)), ct) ?? 0m;

        var lowItems = await AdminInventoryService.Project(
                active.Where(p => p.StockQuantity <= threshold)
                    .OrderBy(p => p.StockQuantity).ThenBy(p => p.NameEn).Take(50),
                threshold)
            .ToListAsync(ct);

        return new InventoryReportDto(total, activeCount, outOfStock, low, threshold, stockValue, lowItems);
    }

    public async Task<CustomerReportDto> GetCustomersAsync(DateOnly? from, DateOnly? to, int take, CancellationToken ct = default)
    {
        var (f, t) = IstTime.ResolveRange(from, to);
        var startUtc = IstTime.StartUtc(f);
        var endUtc = IstTime.EndUtcExclusive(t);
        take = Math.Clamp(take, 1, 50);

        var roleId = await CustomerRoleIdAsync(ct);
        var customers = _db.Users.AsNoTracking()
            .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));

        var totalCustomers = await customers.CountAsync(ct);
        var newCustomers = await customers.CountAsync(u => u.CreatedAt >= startUtc && u.CreatedAt < endUtc, ct);

        var spend = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= startUtc && o.CreatedAt < endUtc
                        && o.PaymentStatus == PaymentStatus.Paid && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.UserId)
            .Select(g => new { UserId = g.Key, Orders = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(x => x.Total)
            .Take(take)
            .ToListAsync(ct);

        var ids = spend.Select(s => s.UserId).ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToDictionaryAsync(u => u.Id, ct);

        var top = spend.Select(s =>
        {
            users.TryGetValue(s.UserId, out var u);
            return new TopCustomerDto(s.UserId, u?.FullName ?? "Unknown", u?.Email ?? string.Empty, s.Orders, s.Total);
        }).ToList();

        return new CustomerReportDto(f, t, totalCustomers, newCustomers, top);
    }

    public async Task<PaymentReportDto> GetPaymentsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var (f, t) = IstTime.ResolveRange(from, to);
        var startUtc = IstTime.StartUtc(f);
        var endUtc = IstTime.EndUtcExclusive(t);

        var inRange = _db.Orders.AsNoTracking().Where(o => o.CreatedAt >= startUtc && o.CreatedAt < endUtc);

        var breakdown = await inRange
            .GroupBy(o => new { o.PaymentMethod, o.PaymentStatus })
            .Select(g => new { g.Key.PaymentMethod, g.Key.PaymentStatus, Orders = g.Count(), Amount = g.Sum(o => o.TotalAmount) })
            .ToListAsync(ct);

        var collected = await inRange.Where(o => o.PaymentStatus == PaymentStatus.Paid)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;
        var refunded = await inRange.Where(o => o.PaymentStatus == PaymentStatus.Refunded)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;
        var awaitingRefund = await inRange.Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;
        var toCollect = await inRange.Where(o => o.PaymentStatus == PaymentStatus.Pending && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var rows = breakdown
            .OrderBy(b => b.PaymentMethod).ThenBy(b => b.PaymentStatus)
            .Select(b => new PaymentBreakdownDto(b.PaymentMethod.ToString(), b.PaymentStatus.ToString(), b.Orders, b.Amount))
            .ToList();

        return new PaymentReportDto(f, t, collected, refunded, awaitingRefund, toCollect, rows);
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = IstTime.Today();
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthStartUtc = IstTime.StartUtc(monthStart);
        const int threshold = InventoryRules.DefaultLowStockThreshold;

        var last30 = await GetSalesAsync(today.AddDays(-29), today, "day", ct);
        var todayRow = last30.Rows[^1];

        var monthRows = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= monthStartUtc && o.Status != OrderStatus.Cancelled)
            .Select(o => new { o.TotalAmount, o.PaymentStatus })
            .ToListAsync(ct);
        var monthRevenue = monthRows.Where(r => r.PaymentStatus == PaymentStatus.Paid).Sum(r => r.TotalAmount);

        var counts = await _db.Orders.AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var byStatus = Enum.GetValues<OrderStatus>()
            .ToDictionary(s => s.ToString(), s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);

        var roleId = await CustomerRoleIdAsync(ct);
        var customers = _db.Users.AsNoTracking()
            .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
        var totalCustomers = await customers.CountAsync(ct);
        var newCustomers = await customers.CountAsync(u => u.CreatedAt >= monthStartUtc, ct);

        var lowStock = await _db.Products.CountAsync(
            p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= threshold, ct);
        var outOfStock = await _db.Products.CountAsync(p => p.IsActive && p.StockQuantity == 0, ct);

        var top = await GetTopProductsAsync(today.AddDays(-29), today, 5, ct);

        var recent = await AdminOrderMapping.ToSummariesAsync(
            _db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).Take(5),
            _db, ct);

        return new DashboardDto(
            todayRow.Orders, todayRow.Revenue, monthRows.Count, monthRevenue,
            byStatus, totalCustomers, newCustomers, lowStock, outOfStock,
            last30.Rows, top, recent);
    }

    private async Task<string> CustomerRoleIdAsync(CancellationToken ct) =>
        await _db.Roles.Where(r => r.Name == AppRoles.Customer)
            .Select(r => r.Id).FirstOrDefaultAsync(ct) ?? string.Empty;
}