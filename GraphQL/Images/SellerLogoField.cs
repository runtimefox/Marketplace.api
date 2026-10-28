using MyApi.Models.Dtos.Images;
using MyApi.Models.Dtos.Sellers;
using MyApi.Services.Interfaces.Images;

namespace MyApi.GraphQL.Images;

[ExtendObjectType<SellerDto>]
public class SellerLogoField
{
    public ImageDto? GetLogo([Parent] SellerDto seller, IImageUrlBuilder imageUrls) =>
        seller.LogoKey is null ? null : imageUrls.Build(seller.LogoKey);
}
