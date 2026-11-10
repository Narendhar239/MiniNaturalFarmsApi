using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Orders;
using MiniNaturalFarms.Domain.Constants;
using MiniNaturalFarms.Domain.Enums;
using MiniNaturalFarms.Infrastructure.Identity;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Admin;

public class AdminCustomerService : IAdminCustomerService
{
    private const int MaxPageSize = 100;
    private readonly AppDbContext _db;

    public AdminCustomerService(AppDbContext db) => _db = db;

    public async Task<PagedResult<CustomerSummaryDto>> GetPagedAsync(CustomerQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var users = await CustomersAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            users = users.Where(u => u.FullName.Contains(s)
                                     || (u.Email != null && u.Email.Contains(s))
                                     || (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
        }

        var total = await users.CountAsync(ct);

        var rows = await users
            .OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.CreatedAt,
                OrderCount = _db.Orders.Count(o => o.UserId == u.Id && o.Status != OrderStatus.Cancelled),
                TotalSpent = _db.Orders
                    .Where(o => o.UserId == u.Id && o.PaymentStatus == PaymentStatus.Paid && o.Status != OrderStatus.Cancelled)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                LastOrderAt = _db.Orders.Where(o => o.UserId == u.Id).Max(o => (DateTime?)o.CreatedAt)
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new CustomerSummaryDto(
            r.Id, r.FullName, r.Email ?? string.Empty, r.PhoneNumber, AdminOrderMapping.AsUtc(r.CreatedAt),
            r.OrderCount, r.TotalSpent,
            r.LastOrderAt.HasValue ? AdminOrderMapping.AsUtc(r.LastOrderAt.Value) : null)).ToList();

        return new PagedResult<CustomerSummaryDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<CustomerDetailDto> GetAsync(string id, CancellationToken ct = default)
    {
        var users = await CustomersAsync(ct);

        var u = await users.Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Email,
                x.PhoneNumber,
                x.CreatedAt,
                OrderCount = _db.Orders.Count(o => o.UserId == x.Id && o.Status != OrderStatus.Cancelled),
                TotalSpent = _db.Orders
                    .Where(o => o.UserId == x.Id && o.PaymentStatus == PaymentStatus.Paid && o.Status != OrderStatus.Cancelled)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                LastOrderAt = _db.Orders.Where(o => o.UserId == x.Id).Max(o => (DateTime?)o.CreatedAt)
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Customer not found.");

        var addressCount = await _db.Addresses.CountAsync(a => a.UserId == id, ct);

        var recent = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Take(10)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CreatedAt,
                o.Status,
                o.PaymentMethod,
                o.PaymentStatus,
                o.TotalAmount,
                ItemCount = o.Items.Sum(i => i.Quantity)
            })
            .ToListAsync(ct);

        var recentOrders = recent.Select(r => new OrderSummaryDto(
            r.Id, r.OrderNumber, AdminOrderMapping.AsUtc(r.CreatedAt),
            r.Status.ToString(), r.PaymentMethod.ToString(), r.PaymentStatus.ToString(),
            r.TotalAmount, r.ItemCount)).ToList();

        return new CustomerDetailDto(
            u.Id, u.FullName, u.Email ?? string.Empty, u.PhoneNumber, AdminOrderMapping.AsUtc(u.CreatedAt),
            u.OrderCount, u.TotalSpent,
            u.LastOrderAt.HasValue ? AdminOrderMapping.AsUtc(u.LastOrderAt.Value) : null,
            addressCount, recentOrders);
    }

    private async Task<IQueryable<ApplicationUser>> CustomersAsync(CancellationToken ct)
    {
        var roleId = await _db.Roles.Where(r => r.Name == AppRoles.Customer)
            .Select(r => r.Id).FirstOrDefaultAsync(ct) ?? string.Empty;

        return _db.Users.AsNoTracking()
            .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
    }
}