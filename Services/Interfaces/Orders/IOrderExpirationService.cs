namespace MyApi.Services.Interfaces.Orders;

public interface IOrderExpirationService
{
    Task<int> CancelExpiredOrdersAsync(CancellationToken ct = default);
}
