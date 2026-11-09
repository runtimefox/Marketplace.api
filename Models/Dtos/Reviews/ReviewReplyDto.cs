using System.Linq.Expressions;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Reviews;

public record ReviewReplyDto
{
    public required Guid Id { get; init; }
    public required string Text { get; init; }
    public required bool IsFromSeller { get; init; }
    public required UserSummaryDto Author { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public static Expression<Func<ReviewReply, ReviewReplyDto>> Projection => x => new ReviewReplyDto
    {
        Id = x.Id,
        Text = x.Text,
        IsFromSeller = x.IsFromSeller,
        Author = new UserSummaryDto
        {
            Id = x.UserAccount.Id,
            Username = x.UserAccount.Username,
            AvatarKey = x.UserAccount.AvatarKey
        },
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
