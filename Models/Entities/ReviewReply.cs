using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("ReviewReplies")]
public class ReviewReply
{
    public const int TextMaxLength = 1000;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Column("ReviewId")]
    public Guid ReviewId { get; private set; }

    public Review Review { get; private set; } = null!;

    [Column("UserAccountId")]
    public Guid UserAccountId { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;

    [Column("IsFromSeller")]
    public bool IsFromSeller { get; private set; }

    [MaxLength(TextMaxLength)]
    [Column("Text")]
    public string Text { get; private set; } = null!;

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    [Column("UpdatedAt")]
    public DateTimeOffset UpdatedAt { get; private set; }

    private ReviewReply()
    {
    }

    public ReviewReply(Guid reviewId, Guid userAccountId, bool isFromSeller, string text)
    {
        if (reviewId == Guid.Empty)
        {
            throw new ArgumentException("Review is required.", nameof(reviewId));
        }

        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userAccountId));
        }

        Id = Guid.CreateVersion7();
        ReviewId = reviewId;
        UserAccountId = userAccountId;
        IsFromSeller = isFromSeller;
        Edit(text);
        CreatedAt = UpdatedAt;
    }

    public void Edit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Reply text is required.", nameof(text));
        }

        text = text.Trim();

        if (text.Length > TextMaxLength)
        {
            throw new ArgumentException($"Length must not exceed {TextMaxLength} characters.", nameof(text));
        }

        Text = text;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
