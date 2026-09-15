using MyApi.Models.Dtos.Images;
using MyApi.Models.Dtos.Products;
using MyApi.Services.Interfaces.Images;

namespace MyApi.GraphQL.Images;

[ExtendObjectType<ProductDto>]
public class ProductImageFields
{
    public ImageDto? GetMainImage([Parent] ProductDto product, IImageUrlBuilder imageUrls) =>
        product.Images.Count == 0 ? null : imageUrls.Build(product.Images[0].StorageKey);
}
