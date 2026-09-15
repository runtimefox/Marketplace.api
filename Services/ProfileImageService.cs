using Microsoft.EntityFrameworkCore;
using MyApi.Models.Dtos.Images;
using MyApi.Services.Interfaces.Images;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class ProfileImageService(
    AppDbContext dbContext,
    ISellerAccessService sellerAccess,
    IImageStorage imageStorage,
    IImageUrlBuilder imageUrls) : IProfileImageService
{
    public async Task<ImageDto> SetAvatarAsync(Guid userId, ImageUpload upload, CancellationToken ct = default)
    {
        var user = await dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == userId, ct)
                   ?? throw new UnauthorizedAccessException("User not found.");

        var storageKey = await imageStorage.SaveAsync(upload, ImageKind.UserAvatar, $"users/{userId}", ct);
        var previousKey = user.AvatarKey;

        user.ChangeAvatar(storageKey);
        await SaveReplacingImageAsync(storageKey, previousKey, ct);

        return imageUrls.Build(storageKey);
    }

    public async Task<bool> DeleteAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user?.AvatarKey is not { } storageKey)
        {
            return false;
        }

        user.RemoveAvatar();
        await dbContext.SaveChangesAsync(ct);
        await imageStorage.DeleteAsync(storageKey);

        return true;
    }

    public async Task<ImageDto?> SetSellerLogoAsync(
        Guid userId, Guid sellerId, ImageUpload upload, CancellationToken ct = default)
    {
        var seller = await dbContext.Sellers.FirstOrDefaultAsync(x => x.Id == sellerId, ct);
        if (seller is null)
        {
            return null;
        }

        await sellerAccess.EnsureOwnerAsync(userId, sellerId, ct);

        var storageKey = await imageStorage.SaveAsync(upload, ImageKind.SellerLogo, $"sellers/{sellerId}", ct);
        var previousKey = seller.LogoKey;

        seller.ChangeLogo(storageKey);
        await SaveReplacingImageAsync(storageKey, previousKey, ct);

        return imageUrls.Build(storageKey);
    }

    public async Task<bool> DeleteSellerLogoAsync(Guid userId, Guid sellerId, CancellationToken ct = default)
    {
        var seller = await dbContext.Sellers.FirstOrDefaultAsync(x => x.Id == sellerId, ct);
        if (seller is null)
        {
            return false;
        }

        await sellerAccess.EnsureOwnerAsync(userId, sellerId, ct);

        if (seller.LogoKey is not { } storageKey)
        {
            return false;
        }

        seller.RemoveLogo();
        await dbContext.SaveChangesAsync(ct);
        await imageStorage.DeleteAsync(storageKey);

        return true;
    }

    private async Task SaveReplacingImageAsync(string newKey, string? previousKey, CancellationToken ct)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            await imageStorage.DeleteAsync(newKey);
            throw;
        }

        if (previousKey is not null)
        {
            await imageStorage.DeleteAsync(previousKey);
        }
    }
}
