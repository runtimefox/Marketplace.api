using MyApi.Models.Dtos.Payments;

namespace MyApi.Services.Interfaces.Payments;

public interface IPaymentService
{
    Task<PaymentSessionDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto, CancellationToken ct = default);
    Task<PaymentDto?> GetPaymentByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
