using MyApi.Models.Dtos.Reviews;

namespace MyApi.Services.Interfaces.Reviews;

public interface IReviewService
{
    IQueryable<ReviewDto> QueryProductReviews(Guid productId);
    IQueryable<ReviewDto> QueryUserReviews(Guid userId);
    Task<bool> CanReviewAsync(Guid userId, Guid productId, CancellationToken ct = default);
    Task<ReviewDto> CreateReviewAsync(Guid userId, CreateReviewDto dto, CancellationToken ct = default);
    Task<ReviewDto?> UpdateReviewAsync(Guid userId, Guid id, UpdateReviewDto dto, CancellationToken ct = default);
    Task<bool> DeleteReviewAsync(Guid userId, bool isAdmin, Guid id, CancellationToken ct = default);
}
