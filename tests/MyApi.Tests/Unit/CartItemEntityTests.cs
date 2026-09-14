using MyApi.Models.Entities;

namespace MyApi.Tests.Unit;

public class CartItemEntityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(CartItem.MaxQuantity + 1)]
    public void Constructor_WithQuantityOutOfRange_Throws(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CartItem(Guid.NewGuid(), Guid.NewGuid(), quantity));
    }

    [Fact]
    public void Increase_AddsToQuantity_UpToMaximum()
    {
        var item = new CartItem(Guid.NewGuid(), Guid.NewGuid(), 2);

        item.Increase(3);

        Assert.Equal(5, item.Quantity);
        Assert.Throws<ArgumentOutOfRangeException>(() => item.Increase(CartItem.MaxQuantity));
    }
}
