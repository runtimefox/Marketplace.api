using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("Sellers")]
public class Seller
{
    public const int NameMaxLength = 200;
    public const decimal MinRating = 0m;
    public const decimal MaxRating = 5m;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(NameMaxLength)]
    [Column("Name")]
    public string Name { get; private set; } = null!;

    [Column("Rating")]
    public decimal Rating { get; private set; }

    private readonly List<Product> _products = [];

    public IReadOnlyCollection<Product> Products => _products;

    private readonly List<SellerMember> _members = [];

    public IReadOnlyCollection<SellerMember> Members => _members;

    private Seller()
    {
    }

    public Seller(string name, Guid ownerId)
    {
        Id = Guid.CreateVersion7();
        Rename(name);
        Rating = MinRating;
        _members.Add(new SellerMember(Id, ownerId, SellerMemberRole.Owner));
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Seller name must not be empty.", nameof(name));
        }

        name = name.Trim();

        if (name.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Length must not exceed {NameMaxLength} characters.", nameof(name));
        }

        Name = name;
    }

    public void ChangeRating(decimal rating)
    {
        if (rating < MinRating || rating > MaxRating)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rating), $"Rating must be between {MinRating} and {MaxRating}.");
        }

        Rating = decimal.Round(rating, 2);
    }
}
