using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("ProductImages")]
public class ProductImage
{
    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Column("ProductId")]
    public Guid ProductId { get; private set; }

    [Required]
    [MaxLength(ImageStorageKey.MaxLength)]
    [Column("StorageKey")]
    public string StorageKey { get; private set; } = null!;

    [Column("Position")]
    public int Position { get; private set; }

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    private ProductImage()
    {
    }

    internal ProductImage(Guid productId, string storageKey, int position)
    {
        Id = Guid.CreateVersion7();
        ProductId = productId;
        StorageKey = ImageStorageKey.Validate(storageKey, nameof(storageKey));
        MoveTo(position);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    internal void MoveTo(int position)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position must not be negative.");
        }

        Position = position;
    }
}
