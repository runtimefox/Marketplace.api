using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyApi.Models.Dtos.Categories;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Categories;
using MyApi.Shared.Data;

namespace MyApi.Services;

public class CategoryService(AppDbContext dbContext) : ICategoryService
{
    public IQueryable<CategoryDto> Query() =>
        dbContext.Categories.Select(CategoryDto.Projection);

    public Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default) =>
        Query().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        var category = new Category(dto.Name);

        dbContext.Categories.Add(category);
        await SaveAsync(ct);

        return CategoryDto.FromEntity(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(
        Guid id, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (category is null)
        {
            return null;
        }

        category.Rename(dto.Name);
        await SaveAsync(ct);

        return CategoryDto.FromEntity(category);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (category is null)
        {
            return false;
        }

        var hasProducts = await dbContext.Products.AnyAsync(x => x.CategoryId == id, ct);
        if (hasProducts)
        {
            throw new InvalidOperationException(HasProductsMessage(id));
        }

        dbContext.Categories.Remove(category);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23503" })
        {
            throw new InvalidOperationException(HasProductsMessage(id));
        }

        return true;
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new InvalidOperationException("A category with this name already exists.");
        }
    }

    private static string HasProductsMessage(Guid id) =>
        $"Category {id} has products and cannot be deleted.";
}
