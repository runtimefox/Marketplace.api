using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Users;

public record CreateUserDto
{
    [Required]
    [MaxLength(UserAccount.UsernameMaxLength)]
    public required string Username { get; init; }

    [Required]
    [EmailAddress]
    [MaxLength(UserAccount.EmailMaxLength)]
    public required string Email { get; init; }

    [Required]
    [MinLength(UserAccount.PasswordMinLength)]
    [MaxLength(UserAccount.RawPasswordMaxLength)]
    public required string Password { get; init; }
}
