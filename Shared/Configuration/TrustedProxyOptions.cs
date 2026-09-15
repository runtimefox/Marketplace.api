using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MyApi.Shared.Configuration;

public class TrustedProxyOptions : IValidatableObject
{
    public const string SectionName = "TrustedProxies";

    public string[] Proxies { get; set; } = [];

    public string[] Networks { get; set; } = [];

    [Range(1, 10)]
    public int ForwardLimit { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var proxy in Proxies.Where(x => !IPAddress.TryParse(x, out _)))
        {
            yield return new ValidationResult($"'{proxy}' is not a valid IP address.", [nameof(Proxies)]);
        }

        foreach (var network in Networks.Where(x => !IPNetwork.TryParse(x, out _)))
        {
            yield return new ValidationResult($"'{network}' is not a valid network in CIDR notation.", [nameof(Networks)]);
        }
    }
}
