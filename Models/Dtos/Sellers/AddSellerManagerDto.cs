using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Sellers;

public record AddSellerManagerDto
{
    [Required]
    [EmailAddress]
    [MaxLength(UserAccount.EmailMaxLength)]
    public required string Email { get; init; }
}
