using MyApi.Models.Entities;

namespace MyApi.Tests.Unit;

public class OrderEntityTests
{
    private static readonly Guid SellerId = Guid.NewGuid();

    [Fact]
    public void Create_CalculatesTotal_AndDecreasesStock()
    {
        var phone = NewProduct(stock: 10, price: 99.99m);
        var charger = NewProduct(stock: 5, price: 10m);

        var order = Order.Create(Guid.NewGuid(), SellerId, [(phone, 2), (charger, 3)]);

        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.Equal(229.98m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(8, phone.Stock);
        Assert.Equal(2, charger.Stock);
    }

    [Fact]
    public void Create_WithProductOfAnotherSeller_Throws()
    {
        var foreignProduct = NewProduct(sellerId: Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => Order.Create(Guid.NewGuid(), SellerId, [(foreignProduct, 1)]));
    }

    [Fact]
    public void Create_WithInsufficientStock_Throws()
    {
        var product = NewProduct(stock: 1);

        var exception = Assert.Throws<InvalidOperationException>(
            () => Order.Create(Guid.NewGuid(), SellerId, [(product, 2)]));

        Assert.Contains("Only 1 items", exception.Message);
    }

    [Fact]
    public void Create_WithoutItems_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Order.Create(Guid.NewGuid(), SellerId, []));
    }

    [Fact]
    public void Cancel_ReturnsStock()
    {
        var product = NewProduct(stock: 5);
        var order = Order.Create(Guid.NewGuid(), SellerId, [(product, 3)]);

        order.Cancel();

        Assert.Equal(OrderStatus.Canceled, order.Status);
        Assert.NotNull(order.CanceledAt);
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public void StatusTransitions_FollowOrderLifecycle()
    {
        var order = Order.Create(Guid.NewGuid(), SellerId, [(NewProduct(), 1)]);

        Assert.Throws<InvalidOperationException>(order.Deliver);

        order.Ship();
        Assert.Throws<InvalidOperationException>(order.Cancel);

        order.Deliver();
        Assert.Throws<InvalidOperationException>(order.Ship);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.ShippedAt);
        Assert.NotNull(order.DeliveredAt);
    }

    private static Product NewProduct(int stock = 10, decimal price = 100m, Guid? sellerId = null) =>
        new("Phone", $"sku-{Guid.NewGuid():N}", price, stock, Guid.NewGuid(), sellerId ?? SellerId);
}
