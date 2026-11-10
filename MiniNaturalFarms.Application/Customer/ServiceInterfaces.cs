namespace MiniNaturalFarms.Application.Customer;

public interface ICartService
{
    Task<CartDto> GetAsync(string userId, CancellationToken ct = default);
    Task<CartDto> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken ct = default);
    Task<CartDto> SetQuantityAsync(string userId, int productId, int quantity, CancellationToken ct = default);
    Task<CartDto> RemoveItemAsync(string userId, int productId, CancellationToken ct = default);
    Task ClearAsync(string userId, CancellationToken ct = default);
}

public interface IWishlistService
{
    Task<IReadOnlyList<WishlistItemDto>> GetAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetProductIdsAsync(string userId, CancellationToken ct = default);
    Task AddAsync(string userId, int productId, CancellationToken ct = default);
    Task RemoveAsync(string userId, int productId, CancellationToken ct = default);
}

public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> GetAllAsync(string userId, CancellationToken ct = default);
    Task<AddressDto> GetAsync(string userId, int id, CancellationToken ct = default);
    Task<AddressDto> CreateAsync(string userId, SaveAddressRequest request, CancellationToken ct = default);
    Task<AddressDto> UpdateAsync(string userId, int id, SaveAddressRequest request, CancellationToken ct = default);
    Task<AddressDto> SetDefaultAsync(string userId, int id, CancellationToken ct = default);
    Task DeleteAsync(string userId, int id, CancellationToken ct = default);
}

public interface IProfileService
{
    Task<ProfileDto> GetAsync(string userId, CancellationToken ct = default);
    Task<ProfileDto> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);
}