namespace MyApi.Services.Interfaces.Images;

public interface IImageProcessor
{
    ProcessedImage Process(Stream source, ImageKind kind);
}

public sealed record ProcessedImage(byte[] Small, byte[] Large);
