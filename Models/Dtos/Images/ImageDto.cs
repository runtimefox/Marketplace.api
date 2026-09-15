namespace MyApi.Models.Dtos.Images;

public record ImageDto
{
    public required string SmallUrl { get; init; }
    public required string LargeUrl { get; init; }
}
