using System.ComponentModel.DataAnnotations;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Auth;

public record RegisterSellerDto : CreateUserDto
{
    [Required]
    [MaxLength(Seller.NameMaxLength)]
    public required string SellerName { get; init; }
}
