using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Products;
using MyApi.Services.Interfaces.Products;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Products;

[ExtendObjectType(OperationTypeNames.Query)]
public class ProductQueries
{
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ProductDto> GetProducts(IProductService products) =>
        products.Query();

    public Task<ProductDto?> GetProductById(
        Guid id,
        IProductService products,
        CancellationToken ct) =>
        products.GetProductByIdAsync(id, ct);

    [Authorize]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<ProductDto>> GetSellerProducts(
        Guid sellerId,
        ClaimsPrincipal claimsPrincipal,
        IProductService products,
        CancellationToken ct) =>
        products.QuerySellerProductsAsync(claimsPrincipal.GetRequiredUserId(), sellerId, ct);
}
