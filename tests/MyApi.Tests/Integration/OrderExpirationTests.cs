using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class OrderExpirationTests(MyApiFactory factory)
{
    private const string OrderById = "query ($id: UUID!) { orderById(id: $id) { status createdAt paymentDueAt } }";
    private const string PaymentById = "query ($id: UUID!) { paymentById(id: $id) { status } }";

    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Checkout_SetsPaymentDeadline()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(seller));
        var orderId = (await _scenario.CheckoutAsync(buyer)).Single();

        var order = (await buyer.GraphQLAsync(OrderById, new { id = orderId }))["orderById"];

        var createdAt = order["createdAt"]!.GetValue<DateTimeOffset>();
        var paymentDueAt = order["paymentDueAt"]!.GetValue<DateTimeOffset>();
        Assert.Equal(TimeSpan.FromMinutes(30), paymentDueAt - createdAt);
    }

    [Fact]
    public async Task ExpiredUnpaidOrder_IsCanceled_AndReturnsStock_WhileOrderInTimeIsKept()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        var productId = await _scenario.ProductAsync(seller, stock: 10);
        await _scenario.AddToCartAsync(buyer, productId, 3);
        var expiredOrder = (await _scenario.CheckoutAsync(buyer)).Single();
        await _scenario.AddToCartAsync(buyer, productId, 2);
        var orderInTime = (await _scenario.CheckoutAsync(buyer)).Single();

        await _scenario.MovePaymentDeadlineToPastAsync(expiredOrder);
        await _scenario.CancelExpiredOrdersAsync();

        var expired = await buyer.GraphQLAsync(OrderById, new { id = expiredOrder });
        var inTime = await buyer.GraphQLAsync(OrderById, new { id = orderInTime });
        Assert.Equal("CANCELED", expired["orderById"]["status"].AsString());
        Assert.Equal("CREATED", inTime["orderById"]["status"].AsString());
        Assert.Equal(8, (await _scenario.ProductByIdAsync(productId))["stock"].AsInt());
    }

    [Fact]
    public async Task ExpiredOrder_WithPendingPayment_CancelsPaymentAtProvider()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(seller));
        var orderId = (await _scenario.CheckoutAsync(buyer)).Single();
        var paymentId = await _scenario.CreatePaymentAsync(buyer, [orderId]);

        await _scenario.MovePaymentDeadlineToPastAsync(orderId);
        await _scenario.CancelExpiredOrdersAsync();

        var order = await buyer.GraphQLAsync(OrderById, new { id = orderId });
        var payment = await buyer.GraphQLAsync(PaymentById, new { id = paymentId });
        Assert.Equal("CANCELED", order["orderById"]["status"].AsString());
        Assert.Equal("CANCELED", payment["paymentById"]["status"].AsString());
        Assert.Equal(FakePaymentIntentStatus.Canceled, factory.PaymentGateway.IntentFor(paymentId).Status);
    }

    [Fact]
    public async Task ExpiredOrder_AlreadyChargedAtProvider_IsKept_AndPaidByWebhook()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(seller));
        var orderId = (await _scenario.CheckoutAsync(buyer)).Single();
        var paymentId = await _scenario.CreatePaymentAsync(buyer, [orderId]);
        factory.PaymentGateway.MarkSucceeded(factory.PaymentGateway.IntentFor(paymentId).Id);

        await _scenario.MovePaymentDeadlineToPastAsync(orderId);
        await _scenario.CancelExpiredOrdersAsync();
        var afterExpiration = await buyer.GraphQLAsync(OrderById, new { id = orderId });
        (await _scenario.SendPaymentWebhookAsync(paymentId)).EnsureSuccessStatusCode();
        var afterWebhook = await buyer.GraphQLAsync(OrderById, new { id = orderId });

        Assert.Equal("CREATED", afterExpiration["orderById"]["status"].AsString());
        Assert.Equal("PAID", afterWebhook["orderById"]["status"].AsString());
    }

    [Fact]
    public async Task CreatePayment_AfterDeadline_IsRejected()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(seller));
        var orderId = (await _scenario.CheckoutAsync(buyer)).Single();

        await _scenario.MovePaymentDeadlineToPastAsync(orderId);
        var response = await buyer.GraphQLAsync(Scenario.CreatePaymentMutation, new { orderIds = new[] { orderId } });

        response.AssertError("INVALID_INPUT");
    }
}
