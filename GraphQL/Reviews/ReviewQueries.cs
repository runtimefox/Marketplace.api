using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Reviews;
using MyApi.Services.Interfaces.Reviews;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Reviews;

[ExtendObjectType(OperationTypeNames.Query)]
public class ReviewQueries
{
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ReviewDto> GetProductReviews(
        Guid productId,
        IReviewService reviews) =>
        reviews.QueryProductReviews(productId);

    [Authorize]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ReviewDto> GetMyReviews(
        ClaimsPrincipal claimsPrincipal,
        IReviewService reviews) =>
        reviews.QueryUserReviews(claimsPrincipal.GetRequiredUserId());

    [Authorize]
    public Task<bool> CanReviewProduct(
        Guid productId,
        ClaimsPrincipal claimsPrincipal,
        IReviewService reviews,
        CancellationToken ct) =>
        reviews.CanReviewAsync(claimsPrincipal.GetRequiredUserId(), productId, ct);
}
