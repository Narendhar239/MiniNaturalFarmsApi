using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Admin;

internal static class IstTime
{
    private static readonly TimeSpan Offset = TimeSpan.FromMinutes(330);
    private const int MaxRangeDays = 366;

    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow + Offset);

    public static DateTime StartUtc(DateOnly d) =>
        DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue) - Offset, DateTimeKind.Utc);

    public static DateTime EndUtcExclusive(DateOnly d) => StartUtc(d.AddDays(1));

    public static DateOnly ToIstDate(DateTime utc) => DateOnly.FromDateTime(utc + Offset);

    public static (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var end = to ?? Today();
        var start = from ?? end.AddDays(-29);

        if (start > end)
            throw new BadRequestException("'from' must not be after 'to'.");
        if (end.DayNumber - start.DayNumber >= MaxRangeDays)
            throw new BadRequestException($"Choose a range of at most {MaxRangeDays} days.");

        return (start, end);
    }
}

internal static class AdminOrderMapping
{
    public static DateTime AsUtc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    public static async Task<List<AdminOrderSummaryDto>> ToSummariesAsync(
        IQueryable<Order> orders, AppDbContext db, CancellationToken ct)
    {
        var rows = await orders.Select(o => new
        {
            o.Id,
            o.OrderNumber,
            o.CreatedAt,
            o.ShipFullName,
            Email = db.Users.Where(u => u.Id == o.UserId).Select(u => u.Email).FirstOrDefault(),
            o.Status,
            o.PaymentMethod,
            o.PaymentStatus,
            o.TotalAmount,
            ItemCount = o.Items.Sum(i => i.Quantity)
        }).ToListAsync(ct);

        return rows.Select(r => new AdminOrderSummaryDto(
            r.Id, r.OrderNumber, AsUtc(r.CreatedAt), r.ShipFullName, r.Email ?? string.Empty,
            r.Status.ToString(), r.PaymentMethod.ToString(), r.PaymentStatus.ToString(),
            r.TotalAmount, r.ItemCount)).ToList();
    }
}