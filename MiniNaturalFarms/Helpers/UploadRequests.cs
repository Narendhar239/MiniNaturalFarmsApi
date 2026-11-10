namespace MiniNaturalFarms.Helpers;

public class UploadImageRequest
{
    public IFormFile File { get; set; } = null!;
}

public class UploadImagesRequest
{
    public List<IFormFile> Files { get; set; } = [];
}