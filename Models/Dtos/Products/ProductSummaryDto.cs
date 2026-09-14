namespace MyApi.Models.Dtos.Products;

public record ProductSummaryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
