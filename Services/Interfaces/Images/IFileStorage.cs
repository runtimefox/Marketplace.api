namespace MyApi.Services.Interfaces.Images;

public interface IFileStorage
{
    Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
