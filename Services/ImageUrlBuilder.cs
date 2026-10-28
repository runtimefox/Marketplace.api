using Microsoft.Extensions.Options;
using MyApi.Models.Dtos.Images;
using MyApi.Services.Interfaces.Images;
using MyApi.Shared.Configuration;

namespace MyApi.Services;

public class ImageUrlBuilder(IOptions<StorageOptions> options) : IImageUrlBuilder
{
    public ImageDto Build(string storageKey)
    {
        var baseUrl = options.Value.PublicBaseUrl.TrimEnd('/');

        return new ImageDto
        {
            SmallUrl = $"{baseUrl}/{ImageVariants.SmallKey(storageKey)}",
            LargeUrl = $"{baseUrl}/{ImageVariants.LargeKey(storageKey)}"
        };
    }
}
