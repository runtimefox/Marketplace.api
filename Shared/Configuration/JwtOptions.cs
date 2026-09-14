using System.ComponentModel.DataAnnotations;

namespace MyApi.Shared.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    [Required]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;

    [Range(1, 365)]
    public int RefreshTokenExpiryDays { get; set; } = 30;
}
