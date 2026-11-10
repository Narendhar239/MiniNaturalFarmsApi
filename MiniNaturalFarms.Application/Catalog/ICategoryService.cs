using MiniNaturalFarms.Application.Common;

namespace MiniNaturalFarms.Application.Catalog;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> SetImageAsync(int id, UploadFile file, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}