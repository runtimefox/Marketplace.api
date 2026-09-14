using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class CartAndOrderTests(MyApiFactory factory)
{
    private const string ShipOrder = "mutation ($id: UUID!) { shipOrder(id: $id) { status } }";
    private const string ConfirmDelivery = "mutation ($id: UUID!) { confirmOrderDelivery(id: $id) { status } }";
    private const string CancelOrder = "mutation ($id: UUID!) { cancelOrder(id: $id) { status } }";

    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task AddToCart_SumsQuantity_AndRejectsQuantityAboveStock()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller, stock: 5, price: 10m);
        var buyer = await _scenario.CustomerAsync();

        await _scenario.AddToCartAsync(buyer, productId, 2);
        var cart = await buyer.GraphQLAsync(Scenario.AddToCartMutation, new { productId, quantity = 1 });
        var aboveStock = await buyer.GraphQLAsync(Scenario.AddToCartMutation, new { productId, quantity = 3 });

        Assert.Equal(3, cart["addToCart"]["totalQuantity"].AsInt());
        Assert.Equal(30m, cart["addToCart"]["totalPrice"].AsDecimal());
        aboveStock.AssertError("INVALID_INPUT");
    }

    [Fact]
    public async Task Checkout_CreatesOrderPerSeller_DecreasesStock_AndClearsCart()
    {
        var first = await _scenario.SellerAsync();
        var second = await _scenario.SellerAsync();
        var firstProduct = await _scenario.ProductAsync(first, stock: 10, price: 100m);
        var secondProduct = await _scenario.ProductAsync(second, stock: 10, price: 50m);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, firstProduct, 2);
        await _scenario.AddToCartAsync(buyer, secondProduct, 3);

        var checkout = await buyer.GraphQLAsync(
            "mutation ($input: CheckoutDtoInput!) { checkout(input: $input) { status totalAmount seller { id } } }",
            new { input = Scenario.CheckoutInput() });
        var cart = await buyer.GraphQLAsync("{ myCart { totalQuantity } }");

        var orders = checkout["checkout"].AsArray();
        Assert.Equal(2, orders.Count);
        Assert.All(orders, x => Assert.Equal("CREATED", x!["status"].AsString()));
        Assert.Equal(200m, orders.Single(x => x!["seller"]!["id"].AsGuid() == first.SellerId)!["totalAmount"].AsDecimal());
        Assert.Equal(150m, orders.Single(x => x!["seller"]!["id"].AsGuid() == second.SellerId)!["totalAmount"].AsDecimal());
        Assert.Equal(8, (await _scenario.ProductByIdAsync(firstProduct))["stock"].AsInt());
        Assert.Equal(7, (await _scenario.ProductByIdAsync(secondProduct))["stock"].AsInt());
        Assert.Equal(0, cart["myCart"]["totalQuantity"].AsInt());
    }

    [Fact]
    public async Task Checkout_WithEmptyCart_IsRejected()
    {
        var buyer = await _scenario.CustomerAsync();

        var response = await buyer.GraphQLAsync(Scenario.CheckoutMutation, new { input = Scenario.CheckoutInput() });

        response.AssertError("INVALID_INPUT");
    }

    [Fact]
    public async Task Checkout_WithInvalidPhone_IsRejected_AndKeepsCart()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, productId);

        var response = await buyer.GraphQLAsync(
            Scenario.CheckoutMutation,
            new { input = Scenario.CheckoutInput(phone: "12-34") });
        var cart = await buyer.GraphQLAsync("{ myCart { totalQuantity } }");

        response.AssertError("INVALID_INPUT");
        Assert.Equal(1, cart["myCart"]["totalQuantity"].AsInt());
    }

    [Fact]
    public async Task Order_ExposesNormalizedDeliveryAddressToSeller()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, productId);
        await _scenario.CheckoutAsync(buyer);

        var orders = await seller.Client.GraphQLAsync(
            "query ($sellerId: UUID!) { sellerOrders(sellerId: $sellerId) { nodes { deliveryAddress { recipientName phone country city addressLine apartment postalCode comment } } } }",
            new { sellerId = seller.SellerId });

        var address = Assert.Single(orders["sellerOrders"]["nodes"]!.AsArray())!["deliveryAddress"]!;
        Assert.Equal("Test Buyer", address["recipientName"].AsString());
        Assert.Equal("+14155550142", address["phone"].AsString());
        Assert.Equal("United States", address["country"].AsString());
        Assert.Equal("San Francisco", address["city"].AsString());
        Assert.Equal("12", address["apartment"].AsString());
        Assert.Null(address["comment"]);
    }

    [Fact]
    public async Task DeliveryAddress_CanBeChangedOnlyByBuyerBeforeShipping()
    {
        const string UpdateAddress =
            "mutation ($id: UUID!, $input: DeliveryAddressDtoInput!) { updateOrderDeliveryAddress(id: $id, input: $input) { deliveryAddress { city } } }";

        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, productId);
        var id = (await _scenario.CheckoutAsync(buyer)).Single();
        var newAddress = Scenario.DeliveryAddressInput(city: "Berlin");

        var sellerChanges = await seller.Client.GraphQLAsync(UpdateAddress, new { id, input = newAddress });
        var buyerChanges = await buyer.GraphQLAsync(UpdateAddress, new { id, input = newAddress });
        (await seller.Client.GraphQLAsync(ShipOrder, new { id })).EnsureSuccess();
        var afterShipping = await buyer.GraphQLAsync(UpdateAddress, new { id, input = newAddress });

        sellerChanges.AssertError("FORBIDDEN");
        Assert.Equal("Berlin", buyerChanges["updateOrderDeliveryAddress"]["deliveryAddress"]!["city"].AsString());
        afterShipping.AssertError("INVALID_INPUT");
    }

    [Fact]
    public async Task OrderStatusFlow_IsGuardedByRoles()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, productId);
        var id = (await _scenario.CheckoutAsync(buyer)).Single();

        var buyerShips = await buyer.GraphQLAsync(ShipOrder, new { id });
        var sellerShips = await seller.Client.GraphQLAsync(ShipOrder, new { id });
        var buyerCancelsShipped = await buyer.GraphQLAsync(CancelOrder, new { id });
        var sellerConfirms = await seller.Client.GraphQLAsync(ConfirmDelivery, new { id });
        var buyerConfirms = await buyer.GraphQLAsync(ConfirmDelivery, new { id });

        buyerShips.AssertError("FORBIDDEN");
        Assert.Equal("SHIPPED", sellerShips["shipOrder"]["status"].AsString());
        buyerCancelsShipped.AssertError("INVALID_INPUT");
        sellerConfirms.AssertError("FORBIDDEN");
        Assert.Equal("DELIVERED", buyerConfirms["confirmOrderDelivery"]["status"].AsString());
    }

    [Fact]
    public async Task CancelOrder_ReturnsStock()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller, stock: 5);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, productId, 2);
        var id = (await _scenario.CheckoutAsync(buyer)).Single();
        var stockAfterCheckout = (await _scenario.ProductByIdAsync(productId))["stock"].AsInt();

        var canceled = await buyer.GraphQLAsync(CancelOrder, new { id });

        Assert.Equal(3, stockAfterCheckout);
        Assert.Equal("CANCELED", canceled["cancelOrder"]["status"].AsString());
        Assert.Equal(5, (await _scenario.ProductByIdAsync(productId))["stock"].AsInt());
    }

    [Fact]
    public async Task Orders_AreHiddenFromOtherUsersAndShops()
    {
        var seller = await _scenario.SellerAsync();
        var otherSeller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        var stranger = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, productId);
        var id = (await _scenario.CheckoutAsync(buyer)).Single();

        var strangerOrder = await stranger.GraphQLAsync("query ($id: UUID!) { orderById(id: $id) { id } }", new { id });
        var otherShopOrders = await otherSeller.Client.GraphQLAsync(
            "query ($sellerId: UUID!) { sellerOrders(sellerId: $sellerId) { totalCount } }",
            new { sellerId = seller.SellerId });

        strangerOrder.AssertError("FORBIDDEN");
        otherShopOrders.AssertError("FORBIDDEN");
    }
}
