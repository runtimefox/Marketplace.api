using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApi.Shared.Data;
using MyApi.Tests.Infrastructure;
using SkiaSharp;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class DemoSeedTests(MyApiFactory factory)
{
    private static readonly HttpClient Storage = new();

    [Fact]
    public async Task Seed_CreatesCatalogWithImages_AndIsIdempotent()
    {
        await Seed.RunAsync(factory.Services);
        await Seed.RunAsync(factory.Services);

        var owner = factory.CreateApiClient();
        (await owner.LoginAsync("techstore@myapi.dev")).EnsureSuccessStatusCode();

        var shops = await owner.GraphQLAsync("{ mySellers { seller { id name logo { smallUrl } } } }");
        var shop = Assert.Single(shops["mySellers"].AsArray())!["seller"]!;

        var products = await owner.GraphQLAsync(
            "query ($sellerId: UUID!) { sellerProducts(sellerId: $sellerId, first: 20) { totalCount nodes { sku price images { smallUrl } } } }",
            new { sellerId = shop["id"].AsGuid() });
        var me = await owner.GraphQLAsync("{ me { avatar { smallUrl } } }");

        var nodes = products["sellerProducts"]["nodes"]!.AsArray();
        var firstImageUrl = nodes[0]!["images"]![0]!["smallUrl"].AsString();

        Assert.Equal("TechStore", shop["name"].AsString());
        Assert.NotNull(shop["logo"]);
        Assert.NotNull(me["me"]["avatar"]);
        Assert.Equal(6, products["sellerProducts"]["totalCount"].AsInt());
        Assert.All(nodes, node => Assert.Equal(DemoImages.ProductVariants, node!["images"]!.AsArray().Count));
        Assert.Equal(499.99m, nodes.Single(x => x!["sku"].AsString() == "TS-PHONE-X12")!["price"].AsDecimal());
        Assert.Equal(SKEncodedImageFormat.Webp, TestImages.Describe(await Storage.GetByteArrayAsync(firstImageUrl)).Format);
    }

    [Fact]
    public async Task Seed_ReplacesLegacyPrices_ButKeepsEditedOnes()
    {
        await Seed.RunAsync(factory.Services);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Products
                .Where(x => x.Sku == "TS-EARBUDS-PRO")
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Price, 7990m));
            await dbContext.Products
                .Where(x => x.Sku == "TS-CHARGER-65W")
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Price, 19.99m));
        }

        await Seed.RunAsync(factory.Services);

        await using var check = factory.Services.CreateAsyncScope();
        var prices = await check.ServiceProvider.GetRequiredService<AppDbContext>().Products
            .Where(x => x.Sku == "TS-EARBUDS-PRO" || x.Sku == "TS-CHARGER-65W")
            .ToDictionaryAsync(x => x.Sku, x => x.Price);

        Assert.Equal(79.99m, prices["TS-EARBUDS-PRO"]);
        Assert.Equal(19.99m, prices["TS-CHARGER-65W"]);
    }
}
