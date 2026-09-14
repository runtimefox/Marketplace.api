using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("OrderItems")]
public class OrderItem
{
    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Column("OrderId")]
    public Guid OrderId { get; private set; }

    [Column("ProductId")]
    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    [Required]
    [MaxLength(Product.NameMaxLength)]
    [Column("ProductName")]
    public string ProductName { get; private set; } = null!;

    [Required]
    [MaxLength(Product.SkuMaxLength)]
    [Column("ProductSku")]
    public string ProductSku { get; private set; } = null!;

    [Column("UnitPrice")]
    public decimal UnitPrice { get; private set; }

    [Column("Quantity")]
    public int Quantity { get; private set; }

    private OrderItem()
    {
    }

    internal OrderItem(Guid orderId, Product product, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        Id = Guid.CreateVersion7();
        OrderId = orderId;
        ProductId = product.Id;
        Product = product;
        ProductName = product.Name;
        ProductSku = product.Sku;
        UnitPrice = product.Price;
        Quantity = quantity;
    }
}
