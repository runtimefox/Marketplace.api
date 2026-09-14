namespace MyApi.Shared.Configuration;

public class FrontendOptions
{
    public const string SectionName = "Frontend";
    public const string CorsPolicy = "Frontend";

    public string[] AllowedOrigins { get; set; } = [];
}
