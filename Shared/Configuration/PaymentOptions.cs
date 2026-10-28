using System.ComponentModel.DataAnnotations;

namespace MyApi.Shared.Configuration;

public class PaymentOptions
{
    public const string SectionName = "Payments";

    [Required]
    [RegularExpression("^[a-z]{3}$")]
    public string Currency { get; set; } = "eur";

    [Range(1, 10_080)]
    public int PaymentWindowMinutes { get; set; } = 30;

    [Range(5, 86_400)]
    public int ExpirationCheckIntervalSeconds { get; set; } = 60;

    [Range(1, 1000)]
    public int ExpirationBatchSize { get; set; } = 100;

    public TimeSpan PaymentWindow => TimeSpan.FromMinutes(PaymentWindowMinutes);

    public TimeSpan ExpirationCheckInterval => TimeSpan.FromSeconds(ExpirationCheckIntervalSeconds);
}
