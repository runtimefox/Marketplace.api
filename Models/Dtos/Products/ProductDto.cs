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
    public required CategoryDto Category { get; init; }
    public required SellerDto Seller { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }

    public static Expression<Func<Product, ProductDto>> Projection => x => new ProductDto
    {
        Id = x.Id,
        Name = x.Name,
        Sku = x.Sku,
        Price = x.Price,
        Stock = x.Stock,
        IsActive = x.IsActive,
        Category = new CategoryDto
        {
            Id = x.Category.Id,
            Name = x.Category.Name
        },
        Seller = new SellerDto
        {
            Id = x.Seller.Id,
            Name = x.Seller.Name,
            Rating = x.Seller.Rating
        },
        Description = x.Description,
        ImageUrl = x.ImageUrl
    };

    public static ProductDto FromEntity(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Sku = product.Sku,
        Price = product.Price,
        Stock = product.Stock,
        IsActive = product.IsActive,
        Category = CategoryDto.FromEntity(product.Category),
        Seller = SellerDto.FromEntity(product.Seller),
        Description = product.Description,
        ImageUrl = product.ImageUrl
    };
}
