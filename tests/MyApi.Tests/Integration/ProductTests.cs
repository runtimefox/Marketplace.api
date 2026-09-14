using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ProductTests(MyApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task NonMember_CannotCreateProductForShop()
    {
        var seller = await _scenario.SellerAsync();
        var categoryId = await _scenario.CategoryAsync();
        var customer = await _scenario.CustomerAsync();

        var response = await customer.GraphQLAsync(
            Scenario.CreateProductMutation,
            new { input = Scenario.ProductInput(seller.SellerId, categoryId) });

        response.AssertError("FORBIDDEN");
    }

    [Fact]
    public async Task DeactivatedProduct_IsHiddenPublicly_VisibleInCabinet_AndCanBeActivated()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);

        (await seller.Client.GraphQLAsync("mutation ($id: UUID!) { deleteProduct(id: $id) }", new { id = productId }))
            .EnsureSuccess();

        var publicList = await factory.CreateApiClient().GraphQLAsync(
            "query ($id: UUID!) { products(where: { id: { eq: $id } }) { totalCount } }",
            new { id = productId });

        var cabinet = await seller.Client.GraphQLAsync(
            "query ($sellerId: UUID!, $id: UUID!) { sellerProducts(sellerId: $sellerId, where: { id: { eq: $id } }) { nodes { isActive } } }",
            new { sellerId = seller.SellerId, id = productId });

        var activated = await seller.Client.GraphQLAsync(
            "mutation ($id: UUID!) { activateProduct(id: $id) { isActive } }",
            new { id = productId });

        Assert.Equal(0, publicList["products"]["totalCount"].AsInt());
        Assert.False(Assert.Single(cabinet["sellerProducts"]["nodes"]!.AsArray())!["isActive"].AsBool());
        Assert.True(activated["activateProduct"]["isActive"].AsBool());
    }
}
