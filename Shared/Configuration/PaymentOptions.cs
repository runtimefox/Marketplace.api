using System.ComponentModel.DataAnnotations;

namespace MyApi.Shared.Configuration;

public class PaymentOptions
{
    public const string SectionName = "Payments";

    [Required]
    [RegularExpression("^[a-z]{3}$")]
    public string Currency { get; set; } = "eur";
}
