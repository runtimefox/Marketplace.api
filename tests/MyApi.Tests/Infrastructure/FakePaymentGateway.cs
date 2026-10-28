using System.Collections.Concurrent;
using MyApi.Services.Interfaces.Payments;

namespace MyApi.Tests.Infrastructure;

public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly ConcurrentDictionary<string, FakePaymentIntent> _intents = new();

    public Task<PaymentIntentResult> CreatePaymentAsync(PaymentIntentRequest request, CancellationToken ct = default)
    {
        var intent = new FakePaymentIntent($"pi_test_{Guid.NewGuid():N}", request);
        _intents[intent.Id] = intent;

        return Task.FromResult(new PaymentIntentResult(intent.Id, intent.ClientSecret));
    }

    public Task<string> GetClientSecretAsync(string providerPaymentId, CancellationToken ct = default) =>
        Task.FromResult(_intents[providerPaymentId].ClientSecret);

    public Task<bool> TryCancelAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var intent = _intents[providerPaymentId];

        if (intent.Status == FakePaymentIntentStatus.Succeeded)
        {
            return Task.FromResult(false);
        }

        intent.Status = FakePaymentIntentStatus.Canceled;

        return Task.FromResult(true);
    }

    public FakePaymentIntent IntentFor(Guid paymentId) =>
        _intents.Values.Single(x => x.Request.PaymentId == paymentId);

    public void MarkSucceeded(string providerPaymentId) =>
        _intents[providerPaymentId].Status = FakePaymentIntentStatus.Succeeded;
}

public enum FakePaymentIntentStatus
{
    RequiresPaymentMethod,
    Succeeded,
    Canceled
}

public sealed class FakePaymentIntent(string id, PaymentIntentRequest request)
{
    public string Id { get; } = id;

    public string ClientSecret { get; } = $"{id}_secret_test";

    public PaymentIntentRequest Request { get; } = request;

    public FakePaymentIntentStatus Status { get; set; } = FakePaymentIntentStatus.RequiresPaymentMethod;
}
