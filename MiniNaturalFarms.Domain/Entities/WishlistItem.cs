using MiniNaturalFarms.Domain.Common;

namespace MiniNaturalFarms.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
