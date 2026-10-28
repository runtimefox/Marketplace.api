using MyApi.Shared.Data;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ProductSearchTests(MyApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Search_MatchesWordPrefixesInNameAndDescription_NameMatchesFirst()
    {
        var seller = await _scenario.SellerAsync();
        var categoryId = await _scenario.CategoryAsync();

        await CreateAsync(seller, categoryId, "Bluetooth Speaker", description: "Pairs with any headphones and phones.");
        await CreateAsync(seller, categoryId, "Studio Headphones", description: "Closed-back, wireless, 40 hours of battery life.");
        await CreateAsync(seller, categoryId, "Linen Shirt");

        Assert.Equal(new[] { "Studio Headphones", "Bluetooth Speaker" }, await NamesAsync(seller, "headphone"));
        Assert.Equal(new[] { "Studio Headphones" }, await NamesAsync(seller, "WIREL batt"));
        Assert.Equal(new[] { "Linen Shirt" }, await NamesAsync(seller, "shirts"));
        Assert.Empty(await NamesAsync(seller, "headphones shirt"));
    }

    [Fact]
    public async Task Search_FindsExactSku_AndKeepsFiltersAndExplicitSorting()
    {
        var seller = await _scenario.SellerAsync();
        var categoryId = await _scenario.CategoryAsync();

        await CreateAsync(seller, categoryId, "Travel Mug", price: 15m);
        await CreateAsync(seller, categoryId, "Coffee Mug", price: 9m);
        var (_, kettleSku) = await CreateAsync(seller, categoryId, "Tea Kettle", price: 30m);

        var byPrice = await NamesAsync(seller, "mug", arguments: "order: { price: ASC }");
        var filtered = await NamesAsync(seller, "mug", filter: "price: { gte: 10 }");
        var bySku = await NamesAsync(seller, $"  {kettleSku.ToLowerInvariant()} ");

        Assert.Equal(new[] { "Coffee Mug", "Travel Mug" }, byPrice);
        Assert.Equal(new[] { "Travel Mug" }, filtered);
        Assert.Equal(new[] { "Tea Kettle" }, bySku);
    }

    [Fact]
    public async Task Search_HidesInactiveProductsPublicly_ButFindsThemInCabinet()
    {
        var seller = await _scenario.SellerAsync();
        var categoryId = await _scenario.CategoryAsync();
        var (id, _) = await CreateAsync(seller, categoryId, "Vintage Film Camera");

        Assert.Equal(new[] { "Vintage Film Camera" }, await NamesAsync(seller, "film camera"));

        (await seller.Client.GraphQLAsync("mutation ($id: UUID!) { deleteProduct(id: $id) }", new { id }))
            .EnsureSuccess();

        var cabinet = await seller.Client.GraphQLAsync(
            "query ($sellerId: UUID!, $search: String) { sellerProducts(sellerId: $sellerId, search: $search) { nodes { name isActive } } }",
            new { sellerId = seller.SellerId, search = "film camera" });

        Assert.Empty(await NamesAsync(seller, "film camera"));
        var found = Assert.Single(cabinet["sellerProducts"]["nodes"]!.AsArray())!;
        Assert.Equal("Vintage Film Camera", found["name"].AsString());
        Assert.False(found["isActive"].AsBool());
    }

    [Fact]
    public async Task Search_IgnoresQuerySyntax_AndRejectsTooLongInput()
    {
        var seller = await _scenario.SellerAsync();
        var categoryId = await _scenario.CategoryAsync();
        await CreateAsync(seller, categoryId, "Cast Iron Skillet");

        var tooLong = await factory.CreateApiClient().GraphQLAsync(
            "query ($search: String) { products(search: $search) { totalCount } }",
            new { search = new string('a', ProductSearch.MaxLength + 1) });

        Assert.Equal(new[] { "Cast Iron Skillet" }, await NamesAsync(seller, "skillet' & | !(:*"));
        Assert.Empty(await NamesAsync(seller, "&|!():*"));
        tooLong.AssertError("INVALID_INPUT");
    }

    private static async Task<(Guid Id, string Sku)> CreateAsync(
        SellerAccount seller, Guid categoryId, string name, decimal price = 20m, string? description = null)
    {
        var sku = TestData.Sku();

        var response = await seller.Client.GraphQLAsync(Scenario.CreateProductMutation, new
        {
            input = new { name, sku, price, stock = 10, categoryId, sellerId = seller.SellerId, description }
        });

        return (response["createProduct"]["id"].AsGuid(), sku);
    }

    private async Task<IReadOnlyList<string>> NamesAsync(
        SellerAccount seller, string search, string filter = "", string arguments = "")
    {
        var response = await factory.CreateApiClient().GraphQLAsync(
            $"query ($search: String, $sellerId: UUID!) {{ products(search: $search, where: {{ seller: {{ id: {{ eq: $sellerId }} }} {filter} }} {arguments}) {{ nodes {{ name }} }} }}",
            new { search, sellerId = seller.SellerId });

        return response["products"]["nodes"]!.AsArray().Select(x => x!["name"].AsString()).ToList();
    }
}
