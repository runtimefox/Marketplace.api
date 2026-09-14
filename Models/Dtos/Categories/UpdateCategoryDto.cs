using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Categories;

public record UpdateCategoryDto
{
    [Required]
    [MaxLength(Category.NameMaxLength)]
    public required string Name { get; init; }
}
