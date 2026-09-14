using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Auth;

public record LoginDto
{
    [Required]
    [EmailAddress]
    [MaxLength(UserAccount.EmailMaxLength)]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
