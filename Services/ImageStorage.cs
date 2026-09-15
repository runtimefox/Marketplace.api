using MyApi.Services.Interfaces.Images;

namespace MyApi.Services;

public class ImageStorage(
    IImageProcessor processor,
    IFileStorage files,
    ILogger<ImageStorage> logger) : IImageStorage
{
    public async Task<string> SaveAsync(
        ImageUpload upload, ImageKind kind, string keyPrefix, CancellationToken ct = default)
    {
        if (upload.Length <= 0)
        {
            throw new ArgumentException("The file is empty.", nameof(upload));
        }

        if (upload.Length > ImageUploadRules.MaxFileBytes)
        {
            throw new ArgumentException(
                $"The file must not exceed {ImageUploadRules.MaxFileBytes / (1024 * 1024)} MB.", nameof(upload));
        }

        if (!ImageUploadRules.AllowedContentTypes.Contains(upload.ContentType))
        {
            throw new ArgumentException("Only JPEG, PNG and WebP images are allowed.", nameof(upload));
        }

        var processed = processor.Process(upload.Content, kind);
        var storageKey = $"{keyPrefix}/{Guid.CreateVersion7():N}";

        await files.PutAsync(ImageVariants.SmallKey(storageKey), processed.Small, ImageVariants.ContentType, ct);

        try
        {
            await files.PutAsync(ImageVariants.LargeKey(storageKey), processed.Large, ImageVariants.ContentType, ct);
        }
        catch
        {
            await DeleteAsync(storageKey);
            throw;
        }

        return storageKey;
    }

    public async Task DeleteAsync(string storageKey)
    {
        foreach (var key in new[] { ImageVariants.SmallKey(storageKey), ImageVariants.LargeKey(storageKey) })
        {
            try
            {
                await files.DeleteAsync(key);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to delete image file {Key}", key);
            }
        }
    }
}
