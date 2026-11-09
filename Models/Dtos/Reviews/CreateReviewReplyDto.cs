using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Reviews;

public record CreateReviewReplyDto
{
    public required Guid ReviewId { get; init; }

    [MaxLength(ReviewReply.TextMaxLength)]
    public required string Text { get; init; }
}
