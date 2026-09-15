using MyApi.Models.Dtos.Images;

namespace MyApi.Services.Interfaces.Images;

public interface IImageUrlBuilder
{
    ImageDto Build(string storageKey);
}
