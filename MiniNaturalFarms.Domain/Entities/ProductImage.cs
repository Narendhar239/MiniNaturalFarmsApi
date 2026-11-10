using MiniNaturalFarms.Domain.Common;

namespace MiniNaturalFarms.Domain.Entities;

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
