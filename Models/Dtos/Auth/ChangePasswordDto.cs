using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Auth;

public record ChangePasswordDto
{
    [Required]
    public required string CurrentPassword { get; init; }

    [Required]
    [MinLength(UserAccount.PasswordMinLength)]
    [MaxLength(UserAccount.RawPasswordMaxLength)]
    public required string NewPassword { get; init; }
}
