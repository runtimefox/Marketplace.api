using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MyApi.Models.Dtos.Orders;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Orders;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class OrderService : IOrderService
{
    private const string StockChanged =
        "Stock of some products has changed. Please review your cart and try again.";

    private readonly AppDbContext _dbContext;
    private readonly ISellerAccessService _sellerAccess;

    public OrderService(AppDbContext dbContext, ISellerAccessService sellerAccess)
    {
        _dbContext = dbContext;
        _sellerAccess = sellerAccess;
    }

    public IQueryable<OrderDto> QueryBuyerOrders(Guid userId) =>
        QueryOrders(x => x.BuyerId == userId);

    public async Task<IQueryable<OrderDto>> QuerySellerOrdersAsync(
        Guid userId, Guid sellerId, CancellationToken ct = default)
    {
        await _sellerAccess.EnsureMemberAsync(userId, sellerId, ct);

        return QueryOrders(x => x.SellerId == sellerId);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .Where(x => x.Id == id)
            .Select(x => new { x.BuyerId, x.SellerId })
            .FirstOrDefaultAsync(ct);

        if (order is null)
        {
            return null;
        }

        await EnsureBuyerOrMemberAsync(userId, order.BuyerId, order.SellerId, ct);

        return await GetOrderDtoAsync(id, ct);
    }

    public async Task<IReadOnlyList<OrderDto>> CheckoutAsync(Guid userId, CancellationToken ct = default)
    {
        var cartItems = await _dbContext.CartItems
            .Include(x => x.Product)
            .Where(x => x.UserAccountId == userId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        if (cartItems.Count == 0)
        {
            throw new InvalidOperationException("The cart is empty.");
        }

        var orders = cartItems
            .GroupBy(x => x.Product.SellerId)
            .Select(group => Order.Create(userId, group.Key, group.Select(x => (x.Product, x.Quantity))))
            .ToList();

        _dbContext.Orders.AddRange(orders);
        _dbContext.CartItems.RemoveRange(cartItems);

        await SaveAsync(ct);

        var orderIds = orders.Select(x => x.Id).ToList();

        return await QueryOrders(x => orderIds.Contains(x.Id)).ToListAsync(ct);
    }

    public async Task<OrderDto?> ShipOrderAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is null)
        {
            return null;
        }

        await _sellerAccess.EnsureMemberAsync(userId, order.SellerId, ct);

        order.Ship();
        await SaveAsync(ct);

        return await GetOrderDtoAsync(id, ct);
    }

    public async Task<OrderDto?> ConfirmDeliveryAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is null)
        {
            return null;
        }

        if (order.BuyerId != userId)
        {
            throw new UnauthorizedAccessException("Only the buyer can confirm delivery of an order.");
        }

        order.Deliver();
        await SaveAsync(ct);

        return await GetOrderDtoAsync(id, ct);
    }

    public async Task<OrderDto?> CancelOrderAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (order is null)
        {
            return null;
        }

        await EnsureBuyerOrMemberAsync(userId, order.BuyerId, order.SellerId, ct);

        order.Cancel();
        await SaveAsync(ct);

        return await GetOrderDtoAsync(id, ct);
    }

    private async Task EnsureBuyerOrMemberAsync(Guid userId, Guid buyerId, Guid sellerId, CancellationToken ct)
    {
        if (buyerId == userId || await _sellerAccess.IsMemberAsync(userId, sellerId, ct))
        {
            return;
        }

        throw new UnauthorizedAccessException("You do not have access to this order.");
    }

    private Task<OrderDto> GetOrderDtoAsync(Guid id, CancellationToken ct) =>
        QueryOrders(x => x.Id == id).FirstAsync(ct);

    private IQueryable<OrderDto> QueryOrders(Expression<Func<Order, bool>> predicate) =>
        _dbContext.Orders
            .Where(predicate)
            .OrderByDescending(x => x.CreatedAt)
            .Select(OrderDto.Projection);

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(StockChanged);
        }
    }
}
