using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyApi.Models.Dtos.Products;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Products;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _dbContext;
    private readonly ISellerAccessService _sellerAccess;

    public ProductService(AppDbContext dbContext, ISellerAccessService sellerAccess)
    {
        _dbContext = dbContext;
        _sellerAccess = sellerAccess;
    }

    public IQueryable<ProductDto> Query(bool includeInactive = false) =>
        _dbContext.Products
            .Where(x => includeInactive || x.IsActive)
            .Select(ProductDto.Projection);

    public async Task<IQueryable<ProductDto>> QuerySellerProductsAsync(
        Guid userId, Guid sellerId, CancellationToken ct = default)
    {
        await _sellerAccess.EnsureMemberAsync(userId, sellerId, ct);

        return _dbContext.Products
            .Where(x => x.SellerId == sellerId)
            .Select(ProductDto.Projection);
    }

    public Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default) =>
        Query(includeInactive: true).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<ProductDto> CreateProductAsync(
        Guid userId, CreateProductDto dto, CancellationToken ct = default)
    {
        await _sellerAccess.EnsureMemberAsync(userId, dto.SellerId, ct);
        await EnsureCategoryExistsAsync(dto.CategoryId, ct);

        var product = new Product(dto.Name, dto.Sku, dto.Price, dto.Stock, dto.CategoryId, dto.SellerId);
        product.ChangeDescription(dto.Description);
        product.ChangeImageUrl(dto.ImageUrl);

        _dbContext.Products.Add(product);
        await SaveAsync(ct);
        await _dbContext.Entry(product).Reference(x => x.Category).LoadAsync(ct);
        await _dbContext.Entry(product).Reference(x => x.Seller).LoadAsync(ct);

        return ProductDto.FromEntity(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(
        Guid userId, Guid id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (product is null)
        {
            return null;
        }

        await _sellerAccess.EnsureMemberAsync(userId, product.SellerId, ct);
        await EnsureCategoryExistsAsync(dto.CategoryId, ct);

        product.Rename(dto.Name);
        product.ChangePrice(dto.Price);
        product.SetStock(dto.Stock);
        product.ChangeCategory(dto.CategoryId);
        product.ChangeDescription(dto.Description);
        product.ChangeImageUrl(dto.ImageUrl);

        await SaveAsync(ct);

        return await Query(includeInactive: true).FirstAsync(x => x.Id == id, ct);
    }

    public async Task<ProductDto?> ActivateProductAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (product is null)
        {
            return null;
        }

        await _sellerAccess.EnsureMemberAsync(userId, product.SellerId, ct);

        product.Activate();
        await _dbContext.SaveChangesAsync(ct);

        return await Query(includeInactive: true).FirstAsync(x => x.Id == id, ct);
    }

    public async Task<bool> DeleteProductAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (product is null)
        {
            return false;
        }

        await _sellerAccess.EnsureMemberAsync(userId, product.SellerId, ct);

        product.Deactivate();
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken ct)
    {
        var exists = await _dbContext.Categories.AnyAsync(x => x.Id == categoryId, ct);

        if (!exists)
        {
            throw new InvalidOperationException($"Category {categoryId} was not found.");
        }
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new InvalidOperationException("A product with this SKU already exists.");
        }
    }
}
