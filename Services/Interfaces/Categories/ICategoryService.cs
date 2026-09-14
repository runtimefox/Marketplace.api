using MyApi.Models.Dtos.Categories;

namespace MyApi.Services.Interfaces.Categories;

public interface ICategoryService
{
    IQueryable<CategoryDto> Query();

    Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default);

    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct = default);

    Task<CategoryDto?> UpdateCategoryAsync(
        Guid id, UpdateCategoryDto dto, CancellationToken ct = default);

    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken ct = default);
}
