using MyApi.Services.Interfaces.Payments;
using Stripe;

namespace MyApi.Services;

public class StripePaymentGateway(IStripeClient client, ILogger<StripePaymentGateway> logger) : IPaymentGateway
{
    private const string CanceledStatus = "canceled";
    private const string UnexpectedStateCode = "payment_intent_unexpected_state";
    private const string ProviderUnavailable = "The payment provider is unavailable. Please try again later.";

    private readonly PaymentIntentService _paymentIntents = new(client);

    public async Task<PaymentIntentResult> CreatePaymentAsync(PaymentIntentRequest request, CancellationToken ct = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = StripeAmount.ToMinorUnits(request.Amount),
            Currency = request.Currency,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            Metadata = new Dictionary<string, string>
            {
                ["paymentId"] = request.PaymentId.ToString(),
                ["buyerId"] = request.BuyerId.ToString()
            }
        };

        var intent = await CallAsync(
            () => _paymentIntents.CreateAsync(
                options,
                new RequestOptions { IdempotencyKey = request.PaymentId.ToString() },
                ct),
            "create",
            request.PaymentId.ToString());

        return new PaymentIntentResult(intent.Id, intent.ClientSecret);
    }

    public async Task<string> GetClientSecretAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var intent = await CallAsync(
            () => _paymentIntents.GetAsync(providerPaymentId, cancellationToken: ct),
            "get",
            providerPaymentId);

        return intent.ClientSecret;
    }

    public async Task<bool> TryCancelAsync(string providerPaymentId, CancellationToken ct = default)
    {
        try
        {
            await _paymentIntents.CancelAsync(providerPaymentId, cancellationToken: ct);

            return true;
        }
        catch (StripeException exception) when (exception.StripeError?.Code == UnexpectedStateCode)
        {
            var intent = await CallAsync(
                () => _paymentIntents.GetAsync(providerPaymentId, cancellationToken: ct),
                "get",
                providerPaymentId);

            return intent.Status == CanceledStatus;
        }
        catch (StripeException exception)
        {
            logger.LogError(exception, "Stripe failed to cancel payment intent {PaymentIntentId}", providerPaymentId);

            throw new InvalidOperationException(ProviderUnavailable);
        }
    }

    private async Task<PaymentIntent> CallAsync(Func<Task<PaymentIntent>> call, string action, string reference)
    {
        try
        {
            return await call();
        }
        catch (StripeException exception)
        {
            logger.LogError(exception, "Stripe failed to {Action} payment intent for {Reference}", action, reference);

            throw new InvalidOperationException(ProviderUnavailable);
        }
    }
}
