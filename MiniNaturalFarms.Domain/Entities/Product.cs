using MiniNaturalFarms.Domain.Common;

namespace MiniNaturalFarms.Domain.Entities;

public class Product : BaseEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameTe { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionTe { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
