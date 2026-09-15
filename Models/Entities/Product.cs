using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("Products")]
public class Product
{
    public const int NameMaxLength = 200;
    public const int SkuMaxLength = 64;
    public const int DescriptionMaxLength = 4000;
    public const int MaxImages = 10;

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

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    [Column("UpdatedAt")]
    public DateTimeOffset UpdatedAt { get; private set; }

    public uint Version { get; private set; }

    private readonly List<Review> _reviews = [];

    public IReadOnlyCollection<Review> Reviews => _reviews;

    private readonly List<ProductImage> _images = [];

    public IReadOnlyCollection<ProductImage> Images => _images;

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

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        Stock += quantity;
        Touch();
    }

    public void EnsureAvailable(int quantity)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException($"Product {Name} is not available.");
        }

        if (quantity > Stock)
        {
            throw new InvalidOperationException(
                $"Only {Stock} items of {Name} in stock, {quantity} requested.");
        }
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

    public void EnsureCanAddImage()
    {
        if (_images.Count >= MaxImages)
        {
            throw new InvalidOperationException($"A product can have at most {MaxImages} images.");
        }
    }

    public ProductImage AddImage(string storageKey)
    {
        EnsureCanAddImage();

        var position = _images.Count == 0 ? 0 : _images.Max(x => x.Position) + 1;
        var image = new ProductImage(Id, storageKey, position);
        _images.Add(image);

        return image;
    }

    public ProductImage RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(x => x.Id == imageId)
                    ?? throw new InvalidOperationException($"Image {imageId} does not belong to product {Id}.");

        _images.Remove(image);

        var position = 0;
        foreach (var remaining in _images.OrderBy(x => x.Position).ThenBy(x => x.CreatedAt))
        {
            remaining.MoveTo(position++);
        }

        return image;
    }

    public void ReorderImages(IReadOnlyList<Guid> imageIds)
    {
        var isSameSet = imageIds.Count == _images.Count
                        && imageIds.Distinct().Count() == imageIds.Count
                        && imageIds.All(id => _images.Any(x => x.Id == id));

        if (!isSameSet)
        {
            throw new ArgumentException(
                "The new order must list every image of the product exactly once.", nameof(imageIds));
        }

        for (var position = 0; position < imageIds.Count; position++)
        {
            _images.Single(x => x.Id == imageIds[position]).MoveTo(position);
        }
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
