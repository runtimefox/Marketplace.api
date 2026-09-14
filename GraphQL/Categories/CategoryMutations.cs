using HotChocolate.Authorization;
using MyApi.Models.Dtos.Categories;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Categories;

namespace MyApi.GraphQL.Categories;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class CategoryMutations
{
    [Authorize(Roles = new[] { nameof(UserRole.Admin) })]
    public Task<CategoryDto> CreateCategory(
        CreateCategoryDto input,
        ICategoryService categories,
        CancellationToken ct) =>
        categories.CreateCategoryAsync(input, ct);

    [Authorize(Roles = new[] { nameof(UserRole.Admin) })]
    public Task<CategoryDto?> UpdateCategory(
        Guid id,
        UpdateCategoryDto input,
        ICategoryService categories,
        CancellationToken ct) =>
        categories.UpdateCategoryAsync(id, input, ct);

    [Authorize(Roles = new[] { nameof(UserRole.Admin) })]
    public Task<bool> DeleteCategory(
        Guid id,
        ICategoryService categories,
        CancellationToken ct) =>
        categories.DeleteCategoryAsync(id, ct);
}
