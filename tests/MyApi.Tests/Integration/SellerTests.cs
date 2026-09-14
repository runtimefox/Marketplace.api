using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class SellerTests(MyApiFactory factory)
{
    private const string AddManager =
        "mutation ($sellerId: UUID!, $email: String!) { addSellerManager(sellerId: $sellerId, input: { email: $email }) { userId role } }";

    private const string RemoveMember =
        "mutation ($sellerId: UUID!, $userId: UUID!) { removeSellerMember(sellerId: $sellerId, userId: $userId) }";

    private const string SellerProducts =
        "query ($sellerId: UUID!) { sellerProducts(sellerId: $sellerId) { totalCount } }";

    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task RegisterSeller_MakesUserShopOwner()
    {
        var seller = await _scenario.SellerAsync();

        var response = await seller.Client.GraphQLAsync("{ mySellers { seller { id rating } role } }");

        var shop = Assert.Single(response["mySellers"].AsArray())!;
        Assert.Equal(seller.SellerId, shop["seller"]!["id"].AsGuid());
        Assert.Equal(0m, shop["seller"]!["rating"].AsDecimal());
        Assert.Equal("OWNER", shop["role"].AsString());
    }

    [Fact]
    public async Task NonMember_CannotUpdateShop()
    {
        var seller = await _scenario.SellerAsync();
        var customer = await _scenario.CustomerAsync();

        var response = await customer.GraphQLAsync(
            "mutation ($id: UUID!, $name: String!) { updateSeller(id: $id, input: { name: $name }) { name } }",
            new { id = seller.SellerId, name = "Hacked" });

        response.AssertError("FORBIDDEN");
    }

    [Fact]
    public async Task Manager_CanManageProducts_ButCannotManageStaff()
    {
        var owner = await _scenario.SellerAsync();
        var manager = await _scenario.CustomerAsync();
        var another = await _scenario.CustomerAsync();

        var added = await owner.Client.GraphQLAsync(AddManager, new { sellerId = owner.SellerId, email = manager.Email });
        var productId = await _scenario.ProductAsync(owner with { Client = manager });
        var managerAddsStaff = await manager.GraphQLAsync(AddManager, new { sellerId = owner.SellerId, email = another.Email });

        Assert.Equal("MANAGER", added["addSellerManager"]["role"].AsString());
        Assert.NotEqual(Guid.Empty, productId);
        managerAddsStaff.AssertError("FORBIDDEN");
    }

    [Fact]
    public async Task Owner_CannotBeRemoved_ButManagerCanLeave()
    {
        var owner = await _scenario.SellerAsync();
        var manager = await _scenario.CustomerAsync();
        var sellerId = owner.SellerId;

        var added = await owner.Client.GraphQLAsync(AddManager, new { sellerId, email = manager.Email });
        var managerId = added["addSellerManager"]["userId"].AsGuid();

        var members = await owner.Client.GraphQLAsync(
            "query ($sellerId: UUID!) { sellerMembers(sellerId: $sellerId) { userId role } }",
            new { sellerId });
        var ownerId = members["sellerMembers"].AsArray().Single(x => x!["role"].AsString() == "OWNER")!["userId"].AsGuid();

        var removeOwner = await owner.Client.GraphQLAsync(RemoveMember, new { sellerId, userId = ownerId });
        var leave = await manager.GraphQLAsync(RemoveMember, new { sellerId, userId = managerId });
        var afterLeaving = await manager.GraphQLAsync(SellerProducts, new { sellerId });

        removeOwner.AssertError("INVALID_INPUT");
        Assert.True(leave["removeSellerMember"].AsBool());
        afterLeaving.AssertError("FORBIDDEN");
    }
}
