using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Cart;
using MyApi.Services.Interfaces.Cart;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Cart;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class CartMutations
{
    [Authorize]
    public Task<CartDto> AddToCart(
        AddToCartDto input,
        ClaimsPrincipal claimsPrincipal,
        ICartService cart,
        CancellationToken ct) =>
        cart.AddToCartAsync(claimsPrincipal.GetRequiredUserId(), input, ct);

    [Authorize]
    public Task<CartDto> UpdateCartItem(
        UpdateCartItemDto input,
        ClaimsPrincipal claimsPrincipal,
        ICartService cart,
        CancellationToken ct) =>
        cart.UpdateCartItemAsync(claimsPrincipal.GetRequiredUserId(), input, ct);

    [Authorize]
    public Task<CartDto> RemoveFromCart(
        Guid productId,
        ClaimsPrincipal claimsPrincipal,
        ICartService cart,
        CancellationToken ct) =>
        cart.RemoveFromCartAsync(claimsPrincipal.GetRequiredUserId(), productId, ct);

    [Authorize]
    public Task<CartDto> ClearCart(
        ClaimsPrincipal claimsPrincipal,
        ICartService cart,
        CancellationToken ct) =>
        cart.ClearCartAsync(claimsPrincipal.GetRequiredUserId(), ct);
}
