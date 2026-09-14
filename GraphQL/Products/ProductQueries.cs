using MyApi.Models.Dtos.Products;
using MyApi.Services.Interfaces.Products;

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
}
