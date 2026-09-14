using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Cart;
using MyApi.Services.Interfaces.Cart;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Cart;

[ExtendObjectType(OperationTypeNames.Query)]
public class CartQueries
{
    [Authorize]
    public Task<CartDto> GetMyCart(
        ClaimsPrincipal claimsPrincipal,
        ICartService cart,
        CancellationToken ct) =>
        cart.GetCartAsync(claimsPrincipal.GetRequiredUserId(), ct);
}
