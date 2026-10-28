using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Stripe;

namespace MyApi.Tests.Infrastructure;

public static class StripeWebhook
{
    public const string Url = "/api/payments/stripe/webhook";
    public const string Succeeded = "payment_intent.succeeded";
    public const string Canceled = "payment_intent.canceled";

    public static string Payload(string type, string paymentIntentId, long amountReceived, string currency = "eur") =>
        JsonSerializer.Serialize(new
        {
            id = $"evt_test_{Guid.NewGuid():N}",
            @object = "event",
            api_version = StripeConfiguration.ApiVersion,
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            livemode = false,
            pending_webhooks = 1,
            type,
            data = new
            {
                @object = new
                {
                    id = paymentIntentId,
                    @object = "payment_intent",
                    amount = amountReceived,
                    amount_received = amountReceived,
                    currency,
                    status = type == Succeeded ? "succeeded" : "canceled"
                }
            }
        });

    public static HttpRequestMessage Request(string payload, string? secret = MyApiFactory.StripeWebhookSecret)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
        };

        if (secret is not null)
        {
            request.Headers.Add("Stripe-Signature", EventUtility.GenerateSignatureHeader(payload, secret));
        }

        return request;
    }
}
