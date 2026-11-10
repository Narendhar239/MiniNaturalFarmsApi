namespace MiniNaturalFarms.Application.Customer;

public record WishlistItemDto(
    int ProductId, string NameEn, string NameTe, string? ImageUrl,
    decimal Price, decimal? DiscountPrice, bool IsAvailable, DateTime AddedAt);