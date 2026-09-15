using Microsoft.EntityFrameworkCore;
using MyApi.Models.Dtos.Images;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Images;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class ProductImageService(
    AppDbContext dbContext,
    ISellerAccessService sellerAccess,
    IImageStorage imageStorage,
    IImageUrlBuilder imageUrls) : IProductImageService
{
    public async Task<ProductImageResponseDto?> AddAsync(
        Guid userId, Guid productId, ImageUpload upload, CancellationToken ct = default)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product is null)
        {
            return null;
        }

        await sellerAccess.EnsureMemberAsync(userId, product.SellerId, ct);
        product.EnsureCanAddImage();

        var storageKey = await imageStorage.SaveAsync(upload, ImageKind.ProductPhoto, $"products/{productId}", ct);

        try
        {
            var image = product.AddImage(storageKey);
            dbContext.ProductImages.Add(image);
            await dbContext.SaveChangesAsync(ct);

            return ToResponse(image);
        }
        catch
        {
            await imageStorage.DeleteAsync(storageKey);
            throw;
        }
    }

    public async Task<IReadOnlyList<ProductImageResponseDto>?> ReorderAsync(
        Guid userId, Guid productId, IReadOnlyList<Guid> imageIds, CancellationToken ct = default)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product is null)
        {
            return null;
        }

        await sellerAccess.EnsureMemberAsync(userId, product.SellerId, ct);

        product.ReorderImages(imageIds);
        await dbContext.SaveChangesAsync(ct);

        return product.Images.OrderBy(x => x.Position).Select(ToResponse).ToList();
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid productId, Guid imageId, CancellationToken ct = default)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product is null)
        {
            return false;
        }

        await sellerAccess.EnsureMemberAsync(userId, product.SellerId, ct);

        if (product.Images.All(x => x.Id != imageId))
        {
            return false;
        }

        var image = product.RemoveImage(imageId);
        dbContext.ProductImages.Remove(image);
        await dbContext.SaveChangesAsync(ct);

        await imageStorage.DeleteAsync(image.StorageKey);

        return true;
    }

    private Task<Product?> LoadProductAsync(Guid productId, CancellationToken ct) =>
        dbContext.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == productId, ct);

    private ProductImageResponseDto ToResponse(ProductImage image)
    {
        var urls = imageUrls.Build(image.StorageKey);

        return new ProductImageResponseDto
        {
            Id = image.Id,
            Position = image.Position,
            SmallUrl = urls.SmallUrl,
            LargeUrl = urls.LargeUrl
        };
    }
}
