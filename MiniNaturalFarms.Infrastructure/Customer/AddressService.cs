using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Infrastructure.Persistence;

namespace MiniNaturalFarms.Infrastructure.Customer;

public class AddressService : IAddressService
{
    private const int MaxAddresses = 10;
    private readonly AppDbContext _db;

    public AddressService(AppDbContext db) => _db = db;

    private static AddressDto ToDto(Address a) =>
        new(a.Id, a.FullName, a.Phone, a.Line1, a.Line2, a.City, a.State, a.Pincode, a.IsDefault);

    public async Task<IReadOnlyList<AddressDto>> GetAllAsync(string userId, CancellationToken ct = default) =>
        await _db.Addresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressDto(a.Id, a.FullName, a.Phone, a.Line1, a.Line2, a.City, a.State, a.Pincode, a.IsDefault))
            .ToListAsync(ct);

    public async Task<AddressDto> GetAsync(string userId, int id, CancellationToken ct = default) =>
        ToDto(await FindAsync(userId, id, ct));

    public async Task<AddressDto> CreateAsync(string userId, SaveAddressRequest request, CancellationToken ct = default)
    {
        var count = await _db.Addresses.CountAsync(a => a.UserId == userId, ct);
        if (count >= MaxAddresses)
            throw new ConflictException($"You can save at most {MaxAddresses} addresses.");

        var address = new Address { UserId = userId };
        Apply(address, request);
        address.IsDefault = request.IsDefault || count == 0; // first address is always the default

        if (address.IsDefault) await ClearDefaultAsync(userId, ct);

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task<AddressDto> UpdateAsync(string userId, int id, SaveAddressRequest request, CancellationToken ct = default)
    {
        var address = await FindAsync(userId, id, ct);
        Apply(address, request);

        // A default address stays default until another one is chosen
        if (request.IsDefault && !address.IsDefault)
        {
            await ClearDefaultAsync(userId, ct);
            address.IsDefault = true;
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task<AddressDto> SetDefaultAsync(string userId, int id, CancellationToken ct = default)
    {
        var address = await FindAsync(userId, id, ct);
        if (!address.IsDefault)
        {
            await ClearDefaultAsync(userId, ct);
            address.IsDefault = true;
            await _db.SaveChangesAsync(ct);
        }
        return ToDto(address);
    }

    public async Task DeleteAsync(string userId, int id, CancellationToken ct = default)
    {
        var address = await FindAsync(userId, id, ct);
        var wasDefault = address.IsDefault;

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync(ct);

        if (wasDefault)
        {
            var next = await _db.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (next != null)
            {
                next.IsDefault = true;
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task<Address> FindAsync(string userId, int id, CancellationToken ct) =>
        await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
        ?? throw new NotFoundException("Address not found.");

    private Task ClearDefaultAsync(string userId, CancellationToken ct) =>
        _db.Addresses.Where(a => a.UserId == userId && a.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);

    private static void Apply(Address a, SaveAddressRequest r)
    {
        a.FullName = r.FullName.Trim();
        a.Phone = r.Phone.Trim();
        a.Line1 = r.Line1.Trim();
        a.Line2 = string.IsNullOrWhiteSpace(r.Line2) ? null : r.Line2.Trim();
        a.City = r.City.Trim();
        a.State = r.State.Trim();
        a.Pincode = r.Pincode.Trim();
    }
}