using System.Linq.Expressions;
using MyApi.Models.Dtos.Categories;
using MyApi.Models.Dtos.Sellers;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Products;

public record ProductDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Sku { get; init; }
    public required decimal Price { get; init; }
    public required int Stock { get; init; }
    public required bool IsActive { get; init; }
    public required decimal Rating { get; init; }
    public required int ReviewCount { get; init; }
    public required CategoryDto Category { get; init; }
    public required SellerDto Seller { get; init; }
    public required IReadOnlyList<ProductImageDto> Images { get; init; }
    public string? Description { get; init; }

    public static Expression<Func<Product, ProductDto>> Projection => x => new ProductDto
    {
        Id = x.Id,
        Name = x.Name,
        Sku = x.Sku,
        Price = x.Price,
        Stock = x.Stock,
        IsActive = x.IsActive,
        Rating = Math.Round(x.Reviews.Average(r => (decimal?)r.Rating) ?? 0m, 2),
        ReviewCount = x.Reviews.Count(),
        Category = new CategoryDto
        {
            Id = x.Category.Id,
            Name = x.Category.Name
        },
        Seller = new SellerDto
        {
            Id = x.Seller.Id,
            Name = x.Seller.Name,
            Rating = x.Seller.Rating,
            LogoKey = x.Seller.LogoKey
        },
        Images = x.Images
            .OrderBy(i => i.Position)
            .ThenBy(i => i.CreatedAt)
            .Select(i => new ProductImageDto
            {
                Id = i.Id,
                Position = i.Position,
                StorageKey = i.StorageKey
            })
            .ToList(),
        Description = x.Description
    };

    public static ProductDto FromEntity(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Sku = product.Sku,
        Price = product.Price,
        Stock = product.Stock,
        IsActive = product.IsActive,
        Rating = product.Reviews.Count == 0
            ? 0m
            : Math.Round(product.Reviews.Average(r => (decimal)r.Rating), 2),
        ReviewCount = product.Reviews.Count,
        Category = CategoryDto.FromEntity(product.Category),
        Seller = SellerDto.FromEntity(product.Seller),
        Images = product.Images
            .OrderBy(i => i.Position)
            .ThenBy(i => i.CreatedAt)
            .Select(ProductImageDto.FromEntity)
            .ToList(),
        Description = product.Description
    };
}
