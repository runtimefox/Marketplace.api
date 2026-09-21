using MyApi.Models.Dtos.Products;

namespace MyApi.Services.Interfaces.Products;

public interface IProductService
{
    IQueryable<ProductDto> Query(bool includeInactive = false, string? search = null);
    Task<IQueryable<ProductDto>> QuerySellerProductsAsync(Guid userId, Guid sellerId, string? search = null, CancellationToken ct = default);
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateProductAsync(Guid userId, CreateProductDto dto, CancellationToken ct = default);
    Task<ProductDto?> UpdateProductAsync(Guid userId, Guid id, UpdateProductDto dto, CancellationToken ct = default);
    Task<ProductDto?> ActivateProductAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<bool> DeleteProductAsync(Guid userId, Guid id, CancellationToken ct = default);
}
