using System.Linq.Expressions;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Sellers;

public record SellerMemberDto
{
    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required SellerMemberRole Role { get; init; }
    public required DateTimeOffset JoinedAt { get; init; }

    public static Expression<Func<SellerMember, SellerMemberDto>> Projection => x => new SellerMemberDto
    {
        UserId = x.UserAccountId,
        Username = x.UserAccount.Username,
        Email = x.UserAccount.Email,
        Role = x.Role,
        JoinedAt = x.CreatedAt
    };
}
