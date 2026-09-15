using MyApi.Models.Entities;

namespace MyApi.Tests.Unit;

public class ProductEntityTests
{
    [Fact]
    public void Constructor_NormalizesSku_AndActivatesProduct()
    {
        var product = NewProduct(sku: "  phone-x12 ");

        Assert.Equal("PHONE-X12", product.Sku);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void ChangePrice_Negative_Throws()
    {
        var product = NewProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() => product.ChangePrice(-0.01m));
    }

    [Fact]
    public void EnsureAvailable_ForInactiveProduct_Throws()
    {
        var product = NewProduct();
        product.Deactivate();

        Assert.Throws<InvalidOperationException>(() => product.EnsureAvailable(1));
    }

    [Fact]
    public void EnsureAvailable_AboveStock_Throws()
    {
        var product = NewProduct(stock: 2);

        Assert.Throws<InvalidOperationException>(() => product.EnsureAvailable(3));
    }

    [Fact]
    public void DecreaseAndIncreaseStock_ChangeStock()
    {
        var product = NewProduct(stock: 5);

        product.DecreaseStock(2);
        product.IncreaseStock(4);

        Assert.Equal(7, product.Stock);
        Assert.Throws<InvalidOperationException>(() => product.DecreaseStock(8));
    }

    [Fact]
    public void Images_AreAppendedUpToLimit()
    {
        var product = NewProduct();

        var first = product.AddImage("products/p/1");
        var second = product.AddImage("products/p/2");
        for (var index = 2; index < Product.MaxImages; index++)
        {
            product.AddImage($"products/p/{index + 1}");
        }

        Assert.Equal(0, first.Position);
        Assert.Equal(1, second.Position);
        Assert.Throws<InvalidOperationException>(() => product.AddImage("products/p/extra"));
    }

    [Fact]
    public void RemoveImage_CompactsPositions()
    {
        var product = NewProduct();
        var first = product.AddImage("products/p/1");
        var second = product.AddImage("products/p/2");
        var third = product.AddImage("products/p/3");

        product.RemoveImage(first.Id);

        Assert.Equal(0, second.Position);
        Assert.Equal(1, third.Position);
        Assert.Throws<InvalidOperationException>(() => product.RemoveImage(first.Id));
    }

    [Fact]
    public void ReorderImages_RequiresEveryImageExactlyOnce()
    {
        var product = NewProduct();
        var first = product.AddImage("products/p/1");
        var second = product.AddImage("products/p/2");

        product.ReorderImages([second.Id, first.Id]);

        Assert.Equal(0, second.Position);
        Assert.Equal(1, first.Position);
        Assert.Throws<ArgumentException>(() => product.ReorderImages([second.Id]));
        Assert.Throws<ArgumentException>(() => product.ReorderImages([second.Id, second.Id]));
        Assert.Throws<ArgumentException>(() => product.ReorderImages([second.Id, Guid.NewGuid()]));
    }

    private static Product NewProduct(string sku = "sku-1", int stock = 10) =>
        new("Phone", sku, 100m, stock, Guid.NewGuid(), Guid.NewGuid());
}
