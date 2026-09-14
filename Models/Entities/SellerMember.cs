using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("SellerMembers")]
public class SellerMember
{
    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Column("SellerId")]
    public Guid SellerId { get; private set; }

    public Seller Seller { get; private set; } = null!;

    [Column("UserAccountId")]
    public Guid UserAccountId { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;

    [Column("Role")]
    public SellerMemberRole Role { get; private set; }

    [Column("CreatedAt")]
    public DateTimeOffset CreatedAt { get; private set; }

    private SellerMember()
    {
    }

    internal SellerMember(Guid sellerId, Guid userAccountId, SellerMemberRole role)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userAccountId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), "Unknown seller member role.");
        }

        Id = Guid.CreateVersion7();
        SellerId = sellerId;
        UserAccountId = userAccountId;
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
