using MyApi.Models.Dtos.Images;

namespace MyApi.Services.Interfaces.Images;

public interface IProfileImageService
{
    Task<ImageDto> SetAvatarAsync(Guid userId, ImageUpload upload, CancellationToken ct = default);
    Task<bool> DeleteAvatarAsync(Guid userId, CancellationToken ct = default);
    Task<ImageDto?> SetSellerLogoAsync(Guid userId, Guid sellerId, ImageUpload upload, CancellationToken ct = default);
    Task<bool> DeleteSellerLogoAsync(Guid userId, Guid sellerId, CancellationToken ct = default);
}
