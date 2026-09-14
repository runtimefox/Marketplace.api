using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApi.Shared.Data;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class StockConcurrencyTests(MyApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task ConcurrentStockChange_IsDetectedByRowVersion()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller, stock: 1);

        await using var firstScope = factory.Services.CreateAsyncScope();
        await using var secondScope = factory.Services.CreateAsyncScope();
        var first = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var firstCopy = await first.Products.SingleAsync(x => x.Id == productId);
        var secondCopy = await second.Products.SingleAsync(x => x.Id == productId);

        firstCopy.DecreaseStock(1);
        await first.SaveChangesAsync();

        secondCopy.DecreaseStock(1);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }
}
