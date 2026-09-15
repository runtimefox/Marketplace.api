using SkiaSharp;

namespace MyApi.Tests.Infrastructure;

public static class TestImages
{
    public static byte[] Create(int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var bitmap = new SKBitmap(width, height);

        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint { Color = SKColors.Orange })
        {
            canvas.Clear(SKColors.SteelBlue);
            canvas.DrawRect(0, 0, width / 2f, height / 2f, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);

        return data.ToArray();
    }

    public static (int Width, int Height, SKEncodedImageFormat Format) Describe(byte[] content)
    {
        using var data = SKData.CreateCopy(content);
        using var codec = SKCodec.Create(data) ?? throw new InvalidOperationException("The content is not an image.");

        return (codec.Info.Width, codec.Info.Height, codec.EncodedFormat);
    }
}
