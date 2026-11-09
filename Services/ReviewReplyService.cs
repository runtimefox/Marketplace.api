using Microsoft.EntityFrameworkCore;
using MyApi.Models.Dtos.Reviews;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Reviews;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class ReviewReplyService(AppDbContext dbContext, ISellerAccessService sellerAccess) : IReviewReplyService
{
    public async Task<ReviewReplyDto> CreateReplyAsync(
        Guid userId, CreateReviewReplyDto dto, CancellationToken ct = default)
    {
        var review = await dbContext.Reviews
                         .Where(x => x.Id == dto.ReviewId)
                         .Select(x => new { x.UserAccountId, x.Product.SellerId })
                         .FirstOrDefaultAsync(ct)
                     ?? throw new InvalidOperationException($"Review {dto.ReviewId} was not found.");

        var isFromSeller = await sellerAccess.IsMemberAsync(userId, review.SellerId, ct);

        if (!isFromSeller && review.UserAccountId != userId)
        {
            throw new UnauthorizedAccessException("Only the review author or the shop can reply to a review.");
        }

        var reply = new ReviewReply(dto.ReviewId, userId, isFromSeller, dto.Text);

        dbContext.ReviewReplies.Add(reply);
        await dbContext.SaveChangesAsync(ct);

        return await GetReplyDtoAsync(reply.Id, ct);
    }

    public async Task<ReviewReplyDto?> UpdateReplyAsync(
        Guid userId, Guid id, UpdateReviewReplyDto dto, CancellationToken ct = default)
    {
        var reply = await dbContext.ReviewReplies.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (reply is null)
        {
            return null;
        }

        if (reply.UserAccountId != userId)
        {
            throw new UnauthorizedAccessException("Only the author can edit a reply.");
        }

        reply.Edit(dto.Text);
        await dbContext.SaveChangesAsync(ct);

        return await GetReplyDtoAsync(id, ct);
    }

    public async Task<bool> DeleteReplyAsync(Guid userId, bool isAdmin, Guid id, CancellationToken ct = default)
    {
        var reply = await dbContext.ReviewReplies.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (reply is null)
        {
            return false;
        }

        if (reply.UserAccountId != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException("Only the author or an admin can delete a reply.");
        }

        dbContext.ReviewReplies.Remove(reply);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }

    private Task<ReviewReplyDto> GetReplyDtoAsync(Guid id, CancellationToken ct) =>
        dbContext.ReviewReplies
            .Where(x => x.Id == id)
            .Select(ReviewReplyDto.Projection)
            .FirstAsync(ct);
}
