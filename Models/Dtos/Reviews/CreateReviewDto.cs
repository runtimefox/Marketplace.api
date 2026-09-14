using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Reviews;

public record CreateReviewDto
{
    public required Guid ProductId { get; init; }

    [Range(Review.MinRating, Review.MaxRating)]
    public required int Rating { get; init; }

    [MaxLength(Review.CommentMaxLength)]
    public string? Comment { get; init; }
}
