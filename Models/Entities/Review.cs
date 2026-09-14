using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("Reviews")]
public class Review
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int CommentMaxLength = 2000;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Column("ProductId")]
    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    [Column("UserAccountId")]
    public Guid UserAccountId { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;

    [Column("Rating")]
    public int Rating { get; private set; }

    [MaxLength(CommentMaxLength)]
    [Column("Comment")]
    public string? Comment { get; private set; }

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    [Column("UpdatedAt")]
    public DateTimeOffset UpdatedAt { get; private set; }

    private Review()
    {
    }

    public Review(Guid productId, Guid userAccountId, int rating, string? comment)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product is required.", nameof(productId));
        }

        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userAccountId));
        }

        Id = Guid.CreateVersion7();
        ProductId = productId;
        UserAccountId = userAccountId;
        Update(rating, comment);
        CreatedAt = UpdatedAt;
    }

    public void Update(int rating, string? comment)
    {
        if (rating < MinRating || rating > MaxRating)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rating), $"Rating must be between {MinRating} and {MaxRating}.");
        }

        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        if (comment is { Length: > CommentMaxLength })
        {
            throw new ArgumentException(
                $"Length must not exceed {CommentMaxLength} characters.", nameof(comment));
        }

        Rating = rating;
        Comment = comment;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
