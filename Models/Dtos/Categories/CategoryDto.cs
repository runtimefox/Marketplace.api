using System.Linq.Expressions;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Categories;

public record CategoryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }

    public static CategoryDto FromEntity(Entities.Category category) => new()
    {
        Id = category.Id,
        Name = category.Name
    };

    public static Expression<Func<Category, CategoryDto>> Projection => x => new CategoryDto
    {
        Id = x.Id,
        Name = x.Name
    };
}