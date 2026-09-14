using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("RefreshTokens")]

public class RefreshToken
{
    public const int TokenHashLength = 64;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(TokenHashLength)]
    [Column("TokenHash")]
    public string TokenHash { get; private set; } = null!;

    [Column("UserId")]
    public Guid UserId { get; private set; }

    public UserAccount User { get; private set; } = null!;

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; private set; }

    [Column("ExpiresAt")]
    public DateTime ExpiresAt { get; private set; }

    [Column("RevokedAt")]
    public DateTime? RevokedAt { get; private set; }

    [NotMapped]
    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    private RefreshToken()
    {
    }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash must not be empty.", nameof(tokenHash));
        }

        if (expiresAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Expiration time must be in UTC.", nameof(expiresAt));
        }

        Id = Guid.CreateVersion7();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public void Revoke() => RevokedAt ??= DateTime.UtcNow;
}
