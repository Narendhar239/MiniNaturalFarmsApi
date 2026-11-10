using System.ComponentModel.DataAnnotations;

namespace MiniNaturalFarms.Application.Catalog;

public record CategoryDto(
    int Id, string NameEn, string NameTe, string? ImageUrl, bool IsActive, int ProductCount);

public class SaveCategoryRequest
{
    [Required, MaxLength(150)] public string NameEn { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string NameTe { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}