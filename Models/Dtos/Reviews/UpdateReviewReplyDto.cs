using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Reviews;

public record UpdateReviewReplyDto
{
    [MaxLength(ReviewReply.TextMaxLength)]
    public required string Text { get; init; }
}
