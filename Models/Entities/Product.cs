using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("Products")]
public class Product
{
    public const int NameMaxLength = 200;
    public const int SkuMaxLength = 64;
    public const int DescriptionMaxLength = 4000;
    public const int ImageUrlMaxLength = 2048;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(NameMaxLength)]
    [Column("Name")]
    public string Name { get; private set; } = null!;

    [Required]
    [MaxLength(SkuMaxLength)]
    [Column("Sku")]
    public string Sku { get; private set; } = null!;

    [Column("Price")]
    public decimal Price { get; private set; }

    [Column("Stock")]
    public int Stock { get; private set; }

    [Column("IsActive")]
    public bool IsActive { get; private set; }

    [Column("CategoryId")]
    public Guid CategoryId { get; private set; }

    public Category Category { get; private set; } = null!;

    [Column("SellerId")]
    public Guid SellerId { get; private set; }

    public Seller Seller { get; private set; } = null!;

    [MaxLength(DescriptionMaxLength)]
    [Column("Description")]
    public string? Description { get; private set; }

    [MaxLength(ImageUrlMaxLength)]
    [Column("ImageUrl")]
    public string? ImageUrl { get; private set; }

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    [Column("UpdatedAt")]
    public DateTimeOffset UpdatedAt { get; private set; }

    private Product()
    {
    }

    public Product(string name, string sku, decimal price, int stock, Guid categoryId, Guid sellerId)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("Seller is required.", nameof(sellerId));
        }

        Id = Guid.CreateVersion7();
        Rename(name);
        SetSku(sku);
        ChangePrice(price);
        SetStock(stock);
        ChangeCategory(categoryId);
        SellerId = sellerId;
        IsActive = true;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string name)
    {
        Name = Require(name, NameMaxLength, nameof(name));
        Touch();
    }

    public void SetSku(string sku)
    {
        Sku = Require(sku, SkuMaxLength, nameof(sku)).ToUpperInvariant();
        Touch();
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must not be negative.");
        }

        Price = decimal.Round(price, 2);
        Touch();
    }

    public void SetStock(int stock)
    {
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock must not be negative.");
        }

        Stock = stock;
        Touch();
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (quantity > Stock)
        {
            throw new InvalidOperationException($"Only {Stock} items in stock, {quantity} requested.");
        }

        Stock -= quantity;
        Touch();
    }

    public void ChangeCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        CategoryId = categoryId;
        Touch();
    }

    public void ChangeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            Description = null;
            Touch();
            return;
        }

        Description = Require(description, DescriptionMaxLength, nameof(description));
        Touch();
    }

    public void ChangeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            ImageUrl = null;
            Touch();
            return;
        }

        imageUrl = imageUrl.Trim();

        if (imageUrl.Length > ImageUrlMaxLength)
        {
            throw new ArgumentException(
                $"Length must not exceed {ImageUrlMaxLength} characters.", nameof(imageUrl));
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "Image URL must be an http(s) address.", nameof(imageUrl));
        }

        ImageUrl = imageUrl;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string Require(string value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", paramName);
        }

        value = value.Trim();

        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Length must not exceed {maxLength} characters.", paramName);
        }

        return value;
    }
}
