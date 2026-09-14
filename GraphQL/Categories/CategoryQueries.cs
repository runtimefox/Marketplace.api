using MyApi.Models.Dtos.Categories;
using MyApi.Services.Interfaces.Categories;

namespace MyApi.GraphQL.Categories;

[ExtendObjectType(OperationTypeNames.Query)]
public class CategoryQueries
{
    [UseFiltering]
    [UseSorting]
    public IQueryable<CategoryDto> GetCategories(ICategoryService categories) =>
        categories.Query();

    public Task<CategoryDto?> GetCategoryById(
        Guid id,
        ICategoryService categories,
        CancellationToken ct) =>
        categories.GetCategoryByIdAsync(id, ct);
}
