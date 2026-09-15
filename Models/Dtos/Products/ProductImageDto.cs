using HotChocolate;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Products;

public record ProductImageDto
{
    public required Guid Id { get; init; }
    public required int Position { get; init; }

    [GraphQLIgnore]
    public required string StorageKey { get; init; }

    public static ProductImageDto FromEntity(ProductImage image) => new()
    {
        Id = image.Id,
        Position = image.Position,
        StorageKey = image.StorageKey
    };
}
