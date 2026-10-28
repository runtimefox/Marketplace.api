using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Orders;
using MyApi.Services.Interfaces.Payments;
using MyApi.Shared.Configuration;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class OrderExpirationService(
    AppDbContext dbContext,
    IPaymentGateway paymentGateway,
    IOptions<PaymentOptions> options,
    ILogger<OrderExpirationService> logger) : IOrderExpirationService
{
    public async Task<int> CancelExpiredOrdersAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var orderIds = await dbContext.Orders
            .Where(x => x.Status == OrderStatus.Created && x.PaymentDueAt <= now)
            .OrderBy(x => x.PaymentDueAt)
            .Select(x => x.Id)
            .Take(options.Value.ExpirationBatchSize)
            .ToListAsync(ct);

        var expired = 0;

        foreach (var orderId in orderIds)
        {
            if (await TryExpireAsync(orderId, now, ct))
            {
                expired++;
            }
        }

        return expired;
    }

    private async Task<bool> TryExpireAsync(Guid orderId, DateTimeOffset now, CancellationToken ct)
    {
        try
        {
            var order = await dbContext.Orders
                .Include(x => x.Payment)
                .Include(x => x.Items)
                .ThenInclude(x => x.Product)
                .FirstAsync(x => x.Id == orderId, ct);

            if (order.Status != OrderStatus.Created)
            {
                return false;
            }

            if (order.Payment is not null)
            {
                await paymentGateway.CancelPendingPaymentAsync(order.Payment, ct);
            }

            order.ExpireUnpaid(now);
            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("Order {OrderId} was canceled because it was not paid in time", orderId);

            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or DbUpdateConcurrencyException)
        {
            logger.LogInformation(exception, "Order {OrderId} was not expired and will be retried later", orderId);

            return false;
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
        }
    }
}
