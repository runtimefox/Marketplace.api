namespace MyApi.Models.Dtos.Images;

public record ProductImageResponseDto
{
    public required Guid Id { get; init; }
    public required int Position { get; init; }
    public required string SmallUrl { get; init; }
    public required string LargeUrl { get; init; }
}
