using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Products;
public record CreateProductDto
{
    [Required]
    [MaxLength(Product.NameMaxLength)]
    public required string Name { get; init; }

    [Required]
    [MaxLength(Product.SkuMaxLength)]
    public required string Sku { get; init; }

    [Range(typeof(decimal), "0", "9999999.99")]
    public required decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public required int Stock { get; init; }

    public required Guid CategoryId { get; init; }

    public required Guid SellerId { get; init; }

    [MaxLength(Product.DescriptionMaxLength)]
    public string? Description { get; init; }

    [MaxLength(Product.ImageUrlMaxLength)]
    [Url]
    public string? ImageUrl { get; init; }
}