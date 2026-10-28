using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyApi.Models.Dtos.Payments;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Payments;
using MyApi.Shared.Configuration;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class PaymentService(
    AppDbContext dbContext,
    IPaymentGateway gateway,
    IPaymentWebhookParser webhookParser,
    IOptions<PaymentOptions> options,
    ILogger<PaymentService> logger) : IPaymentService
{
    private const string OrdersChanged = "The orders were changed by another request. Please try again.";

    public async Task<PaymentSessionDto> CreatePaymentAsync(
        Guid userId, CreatePaymentDto dto, CancellationToken ct = default)
    {
        var orderIds = dto.OrderIds.Distinct().ToList();

        if (orderIds.Count == 0)
        {
            throw new ArgumentException("At least one order is required.", nameof(dto));
        }

        if (orderIds.Count > Payment.MaxOrders)
        {
            throw new ArgumentException($"A payment can cover at most {Payment.MaxOrders} orders.", nameof(dto));
        }

        var orders = await dbContext.Orders
            .Include(x => x.Payment)
            .ThenInclude(x => x!.Orders)
            .Where(x => orderIds.Contains(x.Id))
            .ToListAsync(ct);

        if (orders.Count != orderIds.Count)
        {
            throw new InvalidOperationException("Some of the orders were not found.");
        }

        if (orders.Any(x => x.BuyerId != userId))
        {
            throw new UnauthorizedAccessException("You can pay only for your own orders.");
        }

        if (orders.Any(x => x.Status != OrderStatus.Created))
        {
            throw new InvalidOperationException("Only orders awaiting payment can be paid.");
        }

        var pendingPayments = orders
            .Select(x => x.Payment)
            .OfType<Payment>()
            .Where(x => x.Status == PaymentStatus.Pending)
            .Distinct()
            .ToList();

        if (pendingPayments is [var current] && current.Orders.Count == orders.Count && orders.All(x => x.Payment == current))
        {
            var clientSecret = await gateway.GetClientSecretAsync(current.ProviderPaymentId, ct);

            return await GetSessionAsync(current.Id, clientSecret, ct);
        }

        foreach (var pending in pendingPayments)
        {
            await gateway.CancelPendingPaymentAsync(pending, ct);
        }

        var payment = Payment.Create(userId, options.Value.Currency, orders);
        var intent = await gateway.CreatePaymentAsync(
            new PaymentIntentRequest(payment.Id, userId, payment.Amount, payment.Currency), ct);

        payment.AttachProviderPayment(intent.ProviderPaymentId);
        dbContext.Payments.Add(payment);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception exception)
        {
            await CancelOrphanedIntentAsync(intent.ProviderPaymentId);

            if (exception is DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(OrdersChanged);
            }

            throw;
        }

        return await GetSessionAsync(payment.Id, intent.ClientSecret, ct);
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var buyerId = await dbContext.Payments
            .Where(x => x.Id == id)
            .Select(x => (Guid?)x.BuyerId)
            .FirstOrDefaultAsync(ct);

        if (buyerId is null)
        {
            return null;
        }

        if (buyerId != userId)
        {
            throw new UnauthorizedAccessException("You do not have access to this payment.");
        }

        return await GetPaymentDtoAsync(id, ct);
    }

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var webhookEvent = webhookParser.Parse(payload, signature);
        if (webhookEvent is null)
        {
            return;
        }

        var payment = await dbContext.Payments
            .Include(x => x.Orders)
            .FirstOrDefaultAsync(x => x.ProviderPaymentId == webhookEvent.ProviderPaymentId, ct);

        if (payment is null)
        {
            logger.LogWarning(
                "Received a payment webhook for unknown payment intent {PaymentIntentId}",
                webhookEvent.ProviderPaymentId);

            return;
        }

        try
        {
            switch (webhookEvent.Type)
            {
                case PaymentWebhookEventType.Succeeded:
                    payment.MarkSucceeded(webhookEvent.AmountReceived, webhookEvent.Currency);
                    break;
                case PaymentWebhookEventType.Canceled:
                    payment.Cancel();
                    break;
            }
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError(
                exception,
                "Payment {PaymentId} cannot apply webhook event {EventType}",
                payment.Id,
                webhookEvent.Type);

            throw;
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task CancelOrphanedIntentAsync(string providerPaymentId)
    {
        try
        {
            await gateway.TryCancelAsync(providerPaymentId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to cancel orphaned payment intent {PaymentIntentId}", providerPaymentId);
        }
    }

    private async Task<PaymentSessionDto> GetSessionAsync(Guid paymentId, string clientSecret, CancellationToken ct) =>
        new()
        {
            Payment = await GetPaymentDtoAsync(paymentId, ct),
            ClientSecret = clientSecret
        };

    private Task<PaymentDto> GetPaymentDtoAsync(Guid id, CancellationToken ct) =>
        dbContext.Payments
            .Where(x => x.Id == id)
            .Select(PaymentDto.Projection)
            .FirstAsync(ct);
}
