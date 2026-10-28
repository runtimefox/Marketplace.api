namespace MyApi.Services.Interfaces.Payments;

public interface IPaymentWebhookParser
{
    PaymentWebhookEvent? Parse(string payload, string signature);
}

public enum PaymentWebhookEventType
{
    Succeeded,
    Canceled
}

public sealed record PaymentWebhookEvent(
    PaymentWebhookEventType Type,
    string ProviderPaymentId,
    decimal AmountReceived,
    string Currency);
