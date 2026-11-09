using System.Linq.Expressions;
using MyApi.Models.Dtos.Products;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Reviews;

public record ReviewDto
{
    public required Guid Id { get; init; }
    public required int Rating { get; init; }
    public string? Comment { get; init; }
    public required UserSummaryDto Author { get; init; }
    public required ProductSummaryDto Product { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required IReadOnlyList<ReviewReplyDto> Replies { get; init; }

    public static Expression<Func<Review, ReviewDto>> Projection => x => new ReviewDto
    {
        Id = x.Id,
        Rating = x.Rating,
        Comment = x.Comment,
        Author = new UserSummaryDto
        {
            Id = x.UserAccount.Id,
            Username = x.UserAccount.Username,
            AvatarKey = x.UserAccount.AvatarKey
        },
        Product = new ProductSummaryDto
        {
            Id = x.Product.Id,
            Name = x.Product.Name
        },
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        Replies = x.Replies
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ReviewReplyDto
            {
                Id = r.Id,
                Text = r.Text,
                IsFromSeller = r.IsFromSeller,
                Author = new UserSummaryDto
                {
                    Id = r.UserAccount.Id,
                    Username = r.UserAccount.Username,
                    AvatarKey = r.UserAccount.AvatarKey
                },
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToList()
    };
}
