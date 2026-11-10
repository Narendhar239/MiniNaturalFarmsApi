namespace MiniNaturalFarms.Application.Images;

public record StoredImage(string Url, string PublicId);

public interface IImageService
{
    Task<StoredImage> UploadAsync(Stream content, string fileName, string folder, CancellationToken ct = default);
    Task DeleteAsync(string publicId, CancellationToken ct = default);
}