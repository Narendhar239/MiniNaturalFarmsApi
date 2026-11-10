using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using MiniNaturalFarms.Application.Images;
using MiniNaturalFarms.Infrastructure.Settings;

namespace MiniNaturalFarms.Infrastructure.Catalog;

public class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageService(IOptions<CloudinarySettings> options)
    {
        var s = options.Value;
        _cloudinary = new Cloudinary(new Account(s.CloudName, s.ApiKey, s.ApiSecret));
        _cloudinary.Api.Secure = true;
    }

    public async Task<StoredImage> UploadAsync(Stream content, string fileName, string folder, CancellationToken ct = default)
    {
        var upload = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = folder,
            Transformation = new Transformation()
                .Width(1200).Height(1200).Crop("limit")
                .Quality("auto").FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(upload, ct);
        if (result.Error != null)
            throw new InvalidOperationException($"Image upload failed: {result.Error.Message}");

        return new StoredImage(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteAsync(string publicId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
    }
}