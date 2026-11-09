using System.Security.Claims;
using HotChocolate.Authorization;
using MyApi.Models.Dtos.Reviews;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Reviews;
using MyApi.Shared.Auth;

namespace MyApi.GraphQL.Reviews;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class ReviewReplyMutations
{
    [Authorize]
    public Task<ReviewReplyDto> CreateReviewReply(
        CreateReviewReplyDto input,
        ClaimsPrincipal claimsPrincipal,
        IReviewReplyService replies,
        CancellationToken ct) =>
        replies.CreateReplyAsync(claimsPrincipal.GetRequiredUserId(), input, ct);

    [Authorize]
    public Task<ReviewReplyDto?> UpdateReviewReply(
        Guid id,
        UpdateReviewReplyDto input,
        ClaimsPrincipal claimsPrincipal,
        IReviewReplyService replies,
        CancellationToken ct) =>
        replies.UpdateReplyAsync(claimsPrincipal.GetRequiredUserId(), id, input, ct);

    [Authorize]
    public Task<bool> DeleteReviewReply(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        IReviewReplyService replies,
        CancellationToken ct) =>
        replies.DeleteReplyAsync(
            claimsPrincipal.GetRequiredUserId(),
            claimsPrincipal.IsInRole(nameof(UserRole.Admin)),
            id,
            ct);
}
