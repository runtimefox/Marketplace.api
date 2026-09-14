using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Orders;
using MyApi.Services.Interfaces.Orders;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Orders;

[ExtendObjectType(OperationTypeNames.Query)]
public class OrderQueries
{
    [Authorize]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<OrderDto> GetMyOrders(
        ClaimsPrincipal claimsPrincipal,
        IOrderService orders) =>
        orders.QueryBuyerOrders(claimsPrincipal.GetRequiredUserId());

    [Authorize]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public Task<IQueryable<OrderDto>> GetSellerOrders(
        Guid sellerId,
        ClaimsPrincipal claimsPrincipal,
        IOrderService orders,
        CancellationToken ct) =>
        orders.QuerySellerOrdersAsync(claimsPrincipal.GetRequiredUserId(), sellerId, ct);

    [Authorize]
    public Task<OrderDto?> GetOrderById(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IOrderService orders,
        CancellationToken ct) =>
        orders.GetOrderByIdAsync(claimsPrincipal.GetRequiredUserId(), id, ct);
}
