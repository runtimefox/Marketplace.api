using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Payments;

namespace MyApi.Services;

public static class PaymentGatewayExtensions
{
    public static async Task CancelPendingPaymentAsync(
        this IPaymentGateway gateway, Payment payment, CancellationToken ct = default)
    {
        if (payment.Status != PaymentStatus.Pending)
        {
            return;
        }

        if (!await gateway.TryCancelAsync(payment.ProviderPaymentId, ct))
        {
            throw new InvalidOperationException(
                "The payment has already been completed or is being processed. Please try again later.");
        }

        payment.Cancel();
    }
}
