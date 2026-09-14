using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Reviews;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Reviews;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Reviews;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class ReviewMutations
{
    [Authorize]
    public Task<ReviewDto> CreateReview(
        CreateReviewDto input,
        ClaimsPrincipal claimsPrincipal,
        IReviewService reviews,
        CancellationToken ct) =>
        reviews.CreateReviewAsync(claimsPrincipal.GetRequiredUserId(), input, ct);

    [Authorize]
    public Task<ReviewDto?> UpdateReview(
        Guid id,
        UpdateReviewDto input,
        ClaimsPrincipal claimsPrincipal,
        IReviewService reviews,
        CancellationToken ct) =>
        reviews.UpdateReviewAsync(claimsPrincipal.GetRequiredUserId(), id, input, ct);

    [Authorize]
    public Task<bool> DeleteReview(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IReviewService reviews,
        CancellationToken ct) =>
        reviews.DeleteReviewAsync(
            claimsPrincipal.GetRequiredUserId(),
            claimsPrincipal.IsInRole(nameof(UserRole.Admin)),
            id,
            ct);
}
