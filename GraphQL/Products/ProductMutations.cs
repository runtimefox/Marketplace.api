using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Products;
using MyApi.Services.Interfaces.Products;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Products;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class ProductMutations
{
    [Authorize]
    public Task<ProductDto> CreateProduct(
        CreateProductDto input,
        ClaimsPrincipal claimsPrincipal,
        IProductService products,
        CancellationToken ct) =>
        products.CreateProductAsync(claimsPrincipal.GetRequiredUserId(), input, ct);

    [Authorize]
    public Task<ProductDto?> UpdateProduct(
        Guid id,
        UpdateProductDto input,
        ClaimsPrincipal claimsPrincipal,
        IProductService products,
        CancellationToken ct) =>
        products.UpdateProductAsync(claimsPrincipal.GetRequiredUserId(), id, input, ct);

    [Authorize]
    public Task<ProductDto?> ActivateProduct(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IProductService products,
        CancellationToken ct) =>
        products.ActivateProductAsync(claimsPrincipal.GetRequiredUserId(), id, ct);

    [Authorize]
    public Task<bool> DeleteProduct(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IProductService products,
        CancellationToken ct) =>
        products.DeleteProductAsync(claimsPrincipal.GetRequiredUserId(), id, ct);
}
