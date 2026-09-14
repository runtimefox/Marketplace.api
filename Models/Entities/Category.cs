using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("Categories")]
public class Category
{
    public const int NameMaxLength = 100;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(NameMaxLength)]
    [Column("Name")]
    public string Name { get; private set; } = null!;

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<Product> _products = [];

    public IReadOnlyCollection<Product> Products => _products;

    private Category()
    {
    }

    public Category(string name)
    {
        Id = Guid.CreateVersion7();
        Rename(name);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name must not be empty.", nameof(name));
        }

        name = name.Trim();

        if (name.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Length must not exceed {NameMaxLength} characters.", nameof(name));
        }

        Name = name;
    }
}
