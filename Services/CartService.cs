using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyApi.Models.Dtos.Cart;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Cart;
using MyApi.Services.Interfaces.Products;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _dbContext;
    private readonly IProductService _productService;

    public CartService(AppDbContext dbContext, IProductService productService)
    {
        _dbContext = dbContext;
        _productService = productService;
    }

    public async Task<CartDto> GetCartAsync(Guid userId, CancellationToken ct = default)
    {
        var lines = await _dbContext.CartItems
            .Where(x => x.UserAccountId == userId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { x.ProductId, x.Quantity })
            .ToListAsync(ct);

        var productIds = lines.Select(x => x.ProductId).ToList();
        var products = await _productService.Query(includeInactive: true)
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var items = lines
            .Select(x => CartItemDto.Create(products[x.ProductId], x.Quantity))
            .ToList();

        return CartDto.FromItems(items);
    }

    public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto dto, CancellationToken ct = default)
    {
        var product = await GetProductAsync(dto.ProductId, ct);
        var item = await FindItemAsync(userId, dto.ProductId, ct);

        if (item is null)
        {
            item = new CartItem(userId, product.Id, dto.Quantity);
            _dbContext.CartItems.Add(item);
        }
        else
        {
            item.Increase(dto.Quantity);
        }

        product.EnsureAvailable(item.Quantity);
        await SaveAsync(ct);

        return await GetCartAsync(userId, ct);
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid userId, UpdateCartItemDto dto, CancellationToken ct = default)
    {
        var item = await FindItemAsync(userId, dto.ProductId, ct)
                   ?? throw new InvalidOperationException($"Product {dto.ProductId} is not in the cart.");

        if (dto.Quantity == 0)
        {
            _dbContext.CartItems.Remove(item);
        }
        else
        {
            var product = await GetProductAsync(dto.ProductId, ct);
            item.SetQuantity(dto.Quantity);
            product.EnsureAvailable(item.Quantity);
        }

        await SaveAsync(ct);

        return await GetCartAsync(userId, ct);
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        await _dbContext.CartItems
            .Where(x => x.UserAccountId == userId && x.ProductId == productId)
            .ExecuteDeleteAsync(ct);

        return await GetCartAsync(userId, ct);
    }

    public async Task<CartDto> ClearCartAsync(Guid userId, CancellationToken ct = default)
    {
        await _dbContext.CartItems
            .Where(x => x.UserAccountId == userId)
            .ExecuteDeleteAsync(ct);

        return await GetCartAsync(userId, ct);
    }

    private Task<CartItem?> FindItemAsync(Guid userId, Guid productId, CancellationToken ct) =>
        _dbContext.CartItems.FirstOrDefaultAsync(x => x.UserAccountId == userId && x.ProductId == productId, ct);

    private async Task<Product> GetProductAsync(Guid productId, CancellationToken ct) =>
        await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == productId, ct)
        ?? throw new InvalidOperationException($"Product {productId} was not found.");

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new InvalidOperationException("The cart was changed concurrently. Please try again.");
        }
    }
}
