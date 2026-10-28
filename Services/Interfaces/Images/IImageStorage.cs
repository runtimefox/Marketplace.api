namespace MyApi.Services.Interfaces.Images;

public interface IImageStorage
{
    Task<string> SaveAsync(ImageUpload upload, ImageKind kind, string keyPrefix, CancellationToken ct = default);
    Task DeleteAsync(string storageKey);
}

public sealed record ImageUpload(Stream Content, string ContentType, long Length);
