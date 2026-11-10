using MiniNaturalFarms.Application.Common;

namespace MiniNaturalFarms.Application.Catalog;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(int id, bool includeInactive, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(SaveProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, SaveProductRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);

    Task<ProductDto> AddImagesAsync(int productId, IReadOnlyList<UploadFile> files, CancellationToken ct = default);
    Task<ProductDto> DeleteImageAsync(int productId, int imageId, CancellationToken ct = default);
    Task<ProductDto> SetPrimaryImageAsync(int productId, int imageId, CancellationToken ct = default);
}