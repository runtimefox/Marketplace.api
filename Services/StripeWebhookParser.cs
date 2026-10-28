using Microsoft.Extensions.Options;
using MyApi.Services.Interfaces.Payments;
using MyApi.Shared.Configuration;
using Stripe;

namespace MyApi.Services;

public class StripeWebhookParser(IOptions<StripeOptions> options) : IPaymentWebhookParser
{
    private readonly string _webhookSecret = options.Value.WebhookSecret;

    public PaymentWebhookEvent? Parse(string payload, string signature)
    {
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            throw new ArgumentException("The webhook signature is invalid.", nameof(signature));
        }

        if (stripeEvent.Data.Object is not PaymentIntent intent)
        {
            return null;
        }

        PaymentWebhookEventType? type = stripeEvent.Type switch
        {
            EventTypes.PaymentIntentSucceeded => PaymentWebhookEventType.Succeeded,
            EventTypes.PaymentIntentCanceled => PaymentWebhookEventType.Canceled,
            _ => null
        };

        return type is null
            ? null
            : new PaymentWebhookEvent(
                type.Value,
                intent.Id,
                StripeAmount.FromMinorUnits(intent.AmountReceived),
                intent.Currency);
    }
}
