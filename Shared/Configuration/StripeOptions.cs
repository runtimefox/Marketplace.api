using System.ComponentModel.DataAnnotations;

namespace MyApi.Shared.Configuration;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string WebhookSecret { get; set; } = string.Empty;
}
