using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Users;

public record UpdateProfileDto
{
    [Required]
    [MaxLength(UserAccount.UsernameMaxLength)]
    public required string Username { get; init; }

    [Required]
    [EmailAddress]
    [MaxLength(UserAccount.EmailMaxLength)]
    public required string Email { get; init; }
}
