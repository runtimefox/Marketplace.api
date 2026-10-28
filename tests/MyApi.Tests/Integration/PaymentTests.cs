using System.Net;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PaymentTests(MyApiFactory factory)
{
    private const string PaymentById =
        "query ($id: UUID!) { paymentById(id: $id) { id status amount orderIds succeededAt canceledAt } }";

    private const string OrderById = "query ($id: UUID!) { orderById(id: $id) { status paidAt } }";
    private const string CancelOrder = "mutation ($id: UUID!) { cancelOrder(id: $id) { status } }";

    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task CreatePayment_CoversMultiSellerCheckout_AndWebhookMarksOrdersPaid()
    {
        var first = await _scenario.SellerAsync();
        var second = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(first, price: 100m), 2);
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(second, price: 49.99m));
        var orderIds = await _scenario.CheckoutAsync(buyer);

        var created = await buyer.GraphQLAsync(Scenario.CreatePaymentMutation, new { orderIds });
        var payment = created["createPayment"]["payment"]!;
        var paymentId = payment["id"].AsGuid();
        var webhook = await _scenario.SendPaymentWebhookAsync(paymentId);
        var afterWebhook = await buyer.GraphQLAsync(PaymentById, new { id = paymentId });

        Assert.Equal("PENDING", payment["status"].AsString());
        Assert.Equal(249.99m, payment["amount"].AsDecimal());
        Assert.Equal("eur", payment["currency"].AsString());
        Assert.Equal(orderIds.Order(), payment["orderIds"]!.AsArray().Select(x => x.AsGuid()).Order());
        Assert.Equal(factory.PaymentGateway.IntentFor(paymentId).ClientSecret, created["createPayment"]["clientSecret"].AsString());
        Assert.Equal(24999m, factory.PaymentGateway.IntentFor(paymentId).Request.Amount * 100);
        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
        Assert.Equal("SUCCEEDED", afterWebhook["paymentById"]["status"].AsString());

        foreach (var id in orderIds)
        {
            var order = await buyer.GraphQLAsync(OrderById, new { id });
            Assert.Equal("PAID", order["orderById"]["status"].AsString());
            Assert.NotNull(order["orderById"]["paidAt"]);
        }
    }

    [Fact]
    public async Task CreatePayment_ReusesPendingPayment_ForSameOrders_AndReplacesItForOthers()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        var productId = await _scenario.ProductAsync(seller);
        await _scenario.AddToCartAsync(buyer, productId);
        var firstOrder = (await _scenario.CheckoutAsync(buyer)).Single();
        await _scenario.AddToCartAsync(buyer, productId);
        var secondOrder = (await _scenario.CheckoutAsync(buyer)).Single();

        var initial = await _scenario.CreatePaymentAsync(buyer, [firstOrder, secondOrder]);
        var repeated = await _scenario.CreatePaymentAsync(buyer, [secondOrder, firstOrder]);
        var replacement = await _scenario.CreatePaymentAsync(buyer, [firstOrder]);
        var initialAfterReplacement = await buyer.GraphQLAsync(PaymentById, new { id = initial });

        Assert.Equal(initial, repeated);
        Assert.NotEqual(initial, replacement);
        Assert.Equal("CANCELED", initialAfterReplacement["paymentById"]["status"].AsString());
        Assert.Equal(FakePaymentIntentStatus.Canceled, factory.PaymentGateway.IntentFor(initial).Status);
    }

    [Fact]
    public async Task CreatePayment_RejectsForeignPaidAndUnknownOrders()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        var stranger = await _scenario.CustomerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var paidOrder = await _scenario.PaidOrderAsync(buyer, productId);
        await _scenario.AddToCartAsync(buyer, productId);
        var unpaidOrder = (await _scenario.CheckoutAsync(buyer)).Single();

        var foreign = await stranger.GraphQLAsync(Scenario.CreatePaymentMutation, new { orderIds = new[] { unpaidOrder } });
        var alreadyPaid = await buyer.GraphQLAsync(Scenario.CreatePaymentMutation, new { orderIds = new[] { paidOrder } });
        var unknown = await buyer.GraphQLAsync(Scenario.CreatePaymentMutation, new { orderIds = new[] { Guid.NewGuid() } });
        var empty = await buyer.GraphQLAsync(Scenario.CreatePaymentMutation, new { orderIds = Array.Empty<Guid>() });

        foreign.AssertError("FORBIDDEN");
        alreadyPaid.AssertError("INVALID_INPUT");
        unknown.AssertError("INVALID_INPUT");
        empty.AssertError("INVALID_INPUT");
    }

    [Fact]
    public async Task PaymentById_IsVisibleOnlyToBuyer()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(seller));
        var paymentId = await _scenario.CreatePaymentAsync(buyer, await _scenario.CheckoutAsync(buyer));

        var bySeller = await seller.Client.GraphQLAsync(PaymentById, new { id = paymentId });
        var anonymous = await factory.CreateApiClient().GraphQLAsync(PaymentById, new { id = paymentId });

        bySeller.AssertError("FORBIDDEN");
        anonymous.AssertError("AUTH_NOT_AUTHENTICATED");
    }

    [Fact]
    public async Task Webhook_WithInvalidSignatureOrAmount_IsRejected_AndDoesNotPay()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(seller, price: 30m));
        var orderId = (await _scenario.CheckoutAsync(buyer)).Single();
        var paymentId = await _scenario.CreatePaymentAsync(buyer, [orderId]);
        var intentId = factory.PaymentGateway.IntentFor(paymentId).Id;
        var client = factory.CreateApiClient();

        var payload = StripeWebhook.Payload(StripeWebhook.Succeeded, intentId, 3000);
        var forged = await client.SendAsync(StripeWebhook.Request(payload, secret: "whsec_forged"));
        var unsigned = await client.SendAsync(StripeWebhook.Request(payload, secret: null));
        var wrongAmount = await client.SendAsync(
            StripeWebhook.Request(StripeWebhook.Payload(StripeWebhook.Succeeded, intentId, 100)));
        var order = await buyer.GraphQLAsync(OrderById, new { id = orderId });

        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unsigned.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrongAmount.StatusCode);
        Assert.Equal("CREATED", order["orderById"]["status"].AsString());
    }

    [Fact]
    public async Task Webhook_ForUnknownPaymentIntent_IsAcknowledged()
    {
        var payload = StripeWebhook.Payload(StripeWebhook.Succeeded, $"pi_unknown_{Guid.NewGuid():N}", 1000);

        var response = await factory.CreateApiClient().SendAsync(StripeWebhook.Request(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_CanceledEvent_CancelsPayment_AndOrderCanBePaidAgain()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(seller));
        var orderId = (await _scenario.CheckoutAsync(buyer)).Single();
        var firstPayment = await _scenario.CreatePaymentAsync(buyer, [orderId]);

        var webhook = await _scenario.SendPaymentWebhookAsync(firstPayment, StripeWebhook.Canceled);
        var canceled = await buyer.GraphQLAsync(PaymentById, new { id = firstPayment });
        var secondPayment = await _scenario.PayAsync(buyer, [orderId]);
        var order = await buyer.GraphQLAsync(OrderById, new { id = orderId });

        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
        Assert.Equal("CANCELED", canceled["paymentById"]["status"].AsString());
        Assert.NotEqual(firstPayment, secondPayment);
        Assert.Equal("PAID", order["orderById"]["status"].AsString());
    }

    [Fact]
    public async Task CancelOrder_WithPendingPayment_CancelsPaymentAtProvider()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, await _scenario.ProductAsync(seller));
        var orderId = (await _scenario.CheckoutAsync(buyer)).Single();
        var paymentId = await _scenario.CreatePaymentAsync(buyer, [orderId]);

        var canceled = await buyer.GraphQLAsync(CancelOrder, new { id = orderId });
        var payment = await buyer.GraphQLAsync(PaymentById, new { id = paymentId });

        Assert.Equal("CANCELED", canceled["cancelOrder"]["status"].AsString());
        Assert.Equal("CANCELED", payment["paymentById"]["status"].AsString());
        Assert.Equal(FakePaymentIntentStatus.Canceled, factory.PaymentGateway.IntentFor(paymentId).Status);
    }

    [Fact]
    public async Task CancelOrder_WhenProviderAlreadyCharged_IsRejected()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        var productId = await _scenario.ProductAsync(seller, stock: 5);
        await _scenario.AddToCartAsync(buyer, productId);
        var orderId = (await _scenario.CheckoutAsync(buyer)).Single();
        var paymentId = await _scenario.CreatePaymentAsync(buyer, [orderId]);
        factory.PaymentGateway.MarkSucceeded(factory.PaymentGateway.IntentFor(paymentId).Id);

        var cancel = await buyer.GraphQLAsync(CancelOrder, new { id = orderId });
        var order = await buyer.GraphQLAsync(OrderById, new { id = orderId });

        cancel.AssertError("INVALID_INPUT");
        Assert.Equal("CREATED", order["orderById"]["status"].AsString());
        Assert.Equal(4, (await _scenario.ProductByIdAsync(productId))["stock"].AsInt());
    }

    [Fact]
    public async Task CancelOrder_AfterPayment_IsRejected()
    {
        var seller = await _scenario.SellerAsync();
        var buyer = await _scenario.CustomerAsync();
        var orderId = await _scenario.PaidOrderAsync(buyer, await _scenario.ProductAsync(seller));

        var cancel = await buyer.GraphQLAsync(CancelOrder, new { id = orderId });

        cancel.AssertError("INVALID_INPUT");
    }
}
