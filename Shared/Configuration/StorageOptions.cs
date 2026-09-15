using System.ComponentModel.DataAnnotations;

namespace MyApi.Shared.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    [Url]
    public string ServiceUrl { get; set; } = string.Empty;

    [Required]
    [Url]
    public string PublicBaseUrl { get; set; } = string.Empty;

    [Required]
    public string BucketName { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string Region { get; set; } = "us-east-1";
}
