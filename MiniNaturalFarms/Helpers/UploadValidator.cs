namespace MiniNaturalFarms.Helpers;

public static class UploadValidator
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB per image
    private static readonly string[] Allowed = ["image/jpeg", "image/png", "image/webp"];

    public static string? Validate(IFormFile file)
    {
        if (file.Length == 0) return $"'{file.FileName}' is empty.";
        if (file.Length > MaxBytes) return $"'{file.FileName}' is larger than 5 MB.";
        if (!Allowed.Contains(file.ContentType)) return $"'{file.FileName}' must be a JPEG, PNG or WebP image.";
        return null;
    }
}