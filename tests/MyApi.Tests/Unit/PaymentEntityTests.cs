using MyApi.Models.Entities;

namespace MyApi.Tests.Unit;

public class PaymentEntityTests
{
    private static readonly Guid BuyerId = Guid.NewGuid();

    [Fact]
    public void Create_SumsOrderTotals_AndAttachesOrders()
    {
        var first = NewOrder(price: 100m, quantity: 2);
        var second = NewOrder(price: 49.99m);

        var payment = Payment.Create(BuyerId, " EUR ", [first, second]);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(249.99m, payment.Amount);
        Assert.Equal("eur", payment.Currency);
        Assert.Equal(payment.Id, first.PaymentId);
        Assert.Equal(payment.Id, second.PaymentId);
    }

    [Fact]
    public void Create_WithInvalidInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(BuyerId, "eur", []));
        Assert.Throws<ArgumentException>(() => Payment.Create(BuyerId, "euros", [NewOrder()]));
        Assert.Throws<InvalidOperationException>(() => Payment.Create(Guid.NewGuid(), "eur", [NewOrder()]));
    }

    [Fact]
    public void Create_ForOrderWithPendingPayment_Throws()
    {
        var order = NewOrder();
        Payment.Create(BuyerId, "eur", [order]);

        Assert.Throws<InvalidOperationException>(() => Payment.Create(BuyerId, "eur", [order]));
    }

    [Fact]
    public void Create_AfterPreviousPaymentWasCanceled_ReplacesIt()
    {
        var order = NewOrder();
        var first = Payment.Create(BuyerId, "eur", [order]);
        first.Cancel();

        var second = Payment.Create(BuyerId, "eur", [order]);

        Assert.Equal(second.Id, order.PaymentId);
    }

    [Fact]
    public void MarkSucceeded_MarksOrdersPaid_AndIsIdempotent()
    {
        var order = NewOrder(price: 20m);
        var payment = Payment.Create(BuyerId, "eur", [order]);

        payment.MarkSucceeded(20m, "EUR");
        payment.MarkSucceeded(20m, "eur");

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.NotNull(payment.SucceededAt);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(payment.SucceededAt, order.PaidAt);
        Assert.Throws<InvalidOperationException>(payment.Cancel);
    }

    [Fact]
    public void MarkSucceeded_WithDifferentAmountOrCurrency_Throws()
    {
        var order = NewOrder(price: 20m);
        var payment = Payment.Create(BuyerId, "eur", [order]);

        Assert.Throws<InvalidOperationException>(() => payment.MarkSucceeded(19.99m, "eur"));
        Assert.Throws<InvalidOperationException>(() => payment.MarkSucceeded(20m, "usd"));
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    [Fact]
    public void MarkSucceeded_AfterCancel_Throws()
    {
        var order = NewOrder(price: 20m);
        var payment = Payment.Create(BuyerId, "eur", [order]);

        payment.Cancel();
        payment.Cancel();

        Assert.Equal(PaymentStatus.Canceled, payment.Status);
        Assert.Throws<InvalidOperationException>(() => payment.MarkSucceeded(20m, "eur"));
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    [Fact]
    public void AttachProviderPayment_IsAllowedOnce()
    {
        var payment = Payment.Create(BuyerId, "eur", [NewOrder()]);

        Assert.Throws<ArgumentException>(() => payment.AttachProviderPayment(" "));

        payment.AttachProviderPayment("pi_123");

        Assert.Equal("pi_123", payment.ProviderPaymentId);
        Assert.Throws<InvalidOperationException>(() => payment.AttachProviderPayment("pi_456"));
    }

    private static Order NewOrder(decimal price = 100m, int quantity = 1)
    {
        var sellerId = Guid.NewGuid();
        var product = new Product("Phone", $"sku-{Guid.NewGuid():N}", price, 10, Guid.NewGuid(), sellerId);
        var address = new DeliveryAddress(
            "Test Buyer", "+14155550142", "United States", "San Francisco", "500 Market Street", null, "94105", null);

        return Order.Create(BuyerId, sellerId, address, [(product, quantity)]);
    }
}
