using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Sellers;

public record CreateSellerDto
{
    [Required]
    [MaxLength(Seller.NameMaxLength)]
    public required string Name { get; init; }
}
