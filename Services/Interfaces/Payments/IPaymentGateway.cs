namespace MyApi.Services.Interfaces.Payments;

public interface IPaymentGateway
{
    Task<PaymentIntentResult> CreatePaymentAsync(PaymentIntentRequest request, CancellationToken ct = default);
    Task<string> GetClientSecretAsync(string providerPaymentId, CancellationToken ct = default);
    Task<bool> TryCancelAsync(string providerPaymentId, CancellationToken ct = default);
}

public sealed record PaymentIntentRequest(Guid PaymentId, Guid BuyerId, decimal Amount, string Currency);

public sealed record PaymentIntentResult(string ProviderPaymentId, string ClientSecret);
