using MyApi.Models.Dtos.Reviews;

namespace MyApi.Services.Interfaces.Reviews;

public interface IReviewReplyService
{
    Task<ReviewReplyDto> CreateReplyAsync(Guid userId, CreateReviewReplyDto dto, CancellationToken ct = default);
    Task<ReviewReplyDto?> UpdateReplyAsync(Guid userId, Guid id, UpdateReviewReplyDto dto, CancellationToken ct = default);
    Task<bool> DeleteReplyAsync(Guid userId, bool isAdmin, Guid id, CancellationToken ct = default);
}
