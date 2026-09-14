using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Orders;
using MyApi.Services.Interfaces.Orders;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Orders;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class OrderMutations
{
    [Authorize]
    public Task<IReadOnlyList<OrderDto>> Checkout(
        ClaimsPrincipal claimsPrincipal,
        IOrderService orders,
        CancellationToken ct) =>
        orders.CheckoutAsync(claimsPrincipal.GetRequiredUserId(), ct);

    [Authorize]
    public Task<OrderDto?> ShipOrder(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IOrderService orders,
        CancellationToken ct) =>
        orders.ShipOrderAsync(claimsPrincipal.GetRequiredUserId(), id, ct);

    [Authorize]
    public Task<OrderDto?> ConfirmOrderDelivery(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IOrderService orders,
        CancellationToken ct) =>
        orders.ConfirmDeliveryAsync(claimsPrincipal.GetRequiredUserId(), id, ct);

    [Authorize]
    public Task<OrderDto?> CancelOrder(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IOrderService orders,
        CancellationToken ct) =>
        orders.CancelOrderAsync(claimsPrincipal.GetRequiredUserId(), id, ct);
}
