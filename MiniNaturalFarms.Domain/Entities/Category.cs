using MiniNaturalFarms.Domain.Common;

namespace MiniNaturalFarms.Domain.Entities;

public class Category : BaseEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameTe { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}