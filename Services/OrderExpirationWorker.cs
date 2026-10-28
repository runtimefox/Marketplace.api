using Microsoft.Extensions.Options;
using MyApi.Services.Interfaces.Orders;
using MyApi.Shared.Configuration;

namespace MyApi.Services;

public class OrderExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PaymentOptions> options,
    ILogger<OrderExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.ExpirationCheckInterval);

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var expiration = scope.ServiceProvider.GetRequiredService<IOrderExpirationService>();

                var expired = await expiration.CancelExpiredOrdersAsync(stoppingToken);
                if (expired > 0)
                {
                    logger.LogInformation("Canceled {Count} unpaid orders", expired);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Failed to cancel unpaid orders");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
