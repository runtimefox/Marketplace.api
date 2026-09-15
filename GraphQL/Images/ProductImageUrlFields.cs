using MyApi.Models.Dtos.Products;
using MyApi.Services.Interfaces.Images;

namespace MyApi.GraphQL.Images;

[ExtendObjectType<ProductImageDto>]
public class ProductImageUrlFields
{
    public string GetSmallUrl([Parent] ProductImageDto image, IImageUrlBuilder imageUrls) =>
        imageUrls.Build(image.StorageKey).SmallUrl;

    public string GetLargeUrl([Parent] ProductImageDto image, IImageUrlBuilder imageUrls) =>
        imageUrls.Build(image.StorageKey).LargeUrl;
}
