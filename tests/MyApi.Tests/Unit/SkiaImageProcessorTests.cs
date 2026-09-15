using MyApi.Services;
using MyApi.Services.Interfaces.Images;
using MyApi.Tests.Infrastructure;
using SkiaSharp;

namespace MyApi.Tests.Unit;

public class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new();

    [Fact]
    public void Process_CreatesWebpVariants_KeepingAspectRatio()
    {
        using var source = new MemoryStream(TestImages.Create(2000, 1000));

        var result = _processor.Process(source, ImageKind.ProductPhoto);

        Assert.Equal((320, 160, SKEncodedImageFormat.Webp), TestImages.Describe(result.Small));
        Assert.Equal((1280, 640, SKEncodedImageFormat.Webp), TestImages.Describe(result.Large));
    }

    [Fact]
    public void Process_DoesNotUpscaleSmallImages()
    {
        using var source = new MemoryStream(TestImages.Create(200, 100, SKEncodedImageFormat.Jpeg));

        var result = _processor.Process(source, ImageKind.UserAvatar);

        Assert.Equal((96, 48, SKEncodedImageFormat.Webp), TestImages.Describe(result.Small));
        Assert.Equal((200, 100, SKEncodedImageFormat.Webp), TestImages.Describe(result.Large));
    }

    [Fact]
    public void Process_WithNonImageData_Throws()
    {
        using var source = new MemoryStream("definitely not an image"u8.ToArray());

        Assert.Throws<ArgumentException>(() => _processor.Process(source, ImageKind.ProductPhoto));
    }
}
