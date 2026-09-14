using MyApi.Models.Dtos.Orders;

namespace MyApi.Services.Interfaces.Orders;

public interface IOrderService
{
    IQueryable<OrderDto> QueryBuyerOrders(Guid userId);
    Task<IQueryable<OrderDto>> QuerySellerOrdersAsync(Guid userId, Guid sellerId, CancellationToken ct = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> CheckoutAsync(Guid userId, CancellationToken ct = default);
    Task<OrderDto?> ShipOrderAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<OrderDto?> ConfirmDeliveryAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<OrderDto?> CancelOrderAsync(Guid userId, Guid id, CancellationToken ct = default);
}
