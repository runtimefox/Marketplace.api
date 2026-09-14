using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("CartItems")]
public class CartItem
{
    public const int MaxQuantity = 999;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Column("UserAccountId")]
    public Guid UserAccountId { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;

    [Column("ProductId")]
    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    [Column("Quantity")]
    public int Quantity { get; private set; }

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    [Column("UpdatedAt")]
    public DateTimeOffset UpdatedAt { get; private set; }

    private CartItem()
    {
    }

    public CartItem(Guid userAccountId, Guid productId, int quantity)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userAccountId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product is required.", nameof(productId));
        }

        Id = Guid.CreateVersion7();
        UserAccountId = userAccountId;
        ProductId = productId;
        SetQuantity(quantity);
        CreatedAt = UpdatedAt;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity < 1 || quantity > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity), $"Quantity must be between 1 and {MaxQuantity}.");
        }

        Quantity = quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Increase(int quantity) => SetQuantity(Quantity + quantity);
}
