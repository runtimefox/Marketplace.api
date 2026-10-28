using MyApi.Models.Dtos.Images;

namespace MyApi.Services.Interfaces.Images;

public interface IProductImageService
{
    Task<ProductImageResponseDto?> AddAsync(Guid userId, Guid productId, ImageUpload upload, CancellationToken ct = default);
    Task<IReadOnlyList<ProductImageResponseDto>?> ReorderAsync(Guid userId, Guid productId, IReadOnlyList<Guid> imageIds, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, Guid productId, Guid imageId, CancellationToken ct = default);
}
