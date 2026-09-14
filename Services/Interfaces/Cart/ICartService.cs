using MyApi.Models.Dtos.Cart;

namespace MyApi.Services.Interfaces.Cart;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId, CancellationToken ct = default);
    Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto dto, CancellationToken ct = default);
    Task<CartDto> UpdateCartItemAsync(Guid userId, UpdateCartItemDto dto, CancellationToken ct = default);
    Task<CartDto> RemoveFromCartAsync(Guid userId, Guid productId, CancellationToken ct = default);
    Task<CartDto> ClearCartAsync(Guid userId, CancellationToken ct = default);
}
