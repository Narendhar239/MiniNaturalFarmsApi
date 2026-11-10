using System.ComponentModel.DataAnnotations;

namespace MiniNaturalFarms.Application.Catalog;

public record ProductImageDto(int Id, string Url, bool IsPrimary);

public record ProductDto(
    int Id,
    string NameEn, string NameTe,
    string DescriptionEn, string DescriptionTe,
    decimal Price, decimal? DiscountPrice,
    int StockQuantity, bool IsActive,
    int CategoryId, string CategoryNameEn, string CategoryNameTe,
    List<ProductImageDto> Images);

public class SaveProductRequest : IValidatableObject
{
    [Required, MaxLength(250)] public string NameEn { get; set; } = string.Empty;
    [Required, MaxLength(250)] public string NameTe { get; set; } = string.Empty;
    [MaxLength(4000)] public string DescriptionEn { get; set; } = string.Empty;
    [MaxLength(4000)] public string DescriptionTe { get; set; } = string.Empty;
    [Range(0.01, 10000000)] public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    [Range(0, int.MaxValue)] public int StockQuantity { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (DiscountPrice.HasValue && (DiscountPrice <= 0 || DiscountPrice >= Price))
            yield return new ValidationResult(
                "DiscountPrice must be greater than 0 and less than Price.",
                [nameof(DiscountPrice)]);
    }
}

public class ProductQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    /// <summary>newest (default), price_asc, price_desc, name</summary>
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public bool IncludeInactive { get; set; }
}