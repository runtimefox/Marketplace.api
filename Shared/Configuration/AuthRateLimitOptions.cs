using System.ComponentModel.DataAnnotations;

namespace MyApi.Shared.Configuration;

public class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    [Range(1, 100_000)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
