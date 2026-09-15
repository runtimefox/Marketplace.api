using MyApi.Services.Interfaces.Images;
using SkiaSharp;

namespace MyApi.Services;

public class SkiaImageProcessor : IImageProcessor
{
    private const int WebpQuality = 82;

    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    private static readonly HashSet<SKEncodedImageFormat> SupportedFormats =
    [
        SKEncodedImageFormat.Jpeg,
        SKEncodedImageFormat.Png,
        SKEncodedImageFormat.Webp
    ];

    public ProcessedImage Process(Stream source, ImageKind kind)
    {
        using var data = SKData.Create(source);
        using var codec = data is null ? null : SKCodec.Create(data);

        if (codec is null || !SupportedFormats.Contains(codec.EncodedFormat))
        {
            throw new ArgumentException("The file is not a supported image. Use JPEG, PNG or WebP.", nameof(source));
        }

        if ((long)codec.Info.Width * codec.Info.Height > ImageUploadRules.MaxSourcePixels)
        {
            throw new ArgumentException("The image dimensions are too large.", nameof(source));
        }

        using var decoded = SKBitmap.Decode(codec)
                            ?? throw new ArgumentException("The image could not be decoded.", nameof(source));

        var rotation = RotationDegrees(codec.EncodedOrigin);
        using var rotated = rotation == 0 ? null : Rotate(decoded, rotation);
        var oriented = rotated ?? decoded;

        var (smallSide, largeSide) = ImageVariants.MaxSides(kind);

        return new ProcessedImage(Encode(oriented, smallSide), Encode(oriented, largeSide));
    }

    private static int RotationDegrees(SKEncodedOrigin origin) => origin switch
    {
        SKEncodedOrigin.BottomRight => 180,
        SKEncodedOrigin.RightTop => 90,
        SKEncodedOrigin.LeftBottom => 270,
        _ => 0
    };

    private static SKBitmap Rotate(SKBitmap bitmap, int degrees)
    {
        var swapSides = degrees is 90 or 270;
        var rotated = new SKBitmap(
            swapSides ? bitmap.Height : bitmap.Width,
            swapSides ? bitmap.Width : bitmap.Height);

        using var source = SKImage.FromBitmap(bitmap);
        using var canvas = new SKCanvas(rotated);
        canvas.Translate(rotated.Width / 2f, rotated.Height / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
        canvas.DrawImage(source, 0, 0, Sampling);

        return rotated;
    }

    private static byte[] Encode(SKBitmap bitmap, int maxSide)
    {
        var scale = Math.Min(1d, (double)maxSide / Math.Max(bitmap.Width, bitmap.Height));

        using var resized = scale < 1
            ? bitmap.Resize(
                  new SKImageInfo(
                      Math.Max(1, (int)Math.Round(bitmap.Width * scale)),
                      Math.Max(1, (int)Math.Round(bitmap.Height * scale))),
                  Sampling)
              ?? throw new InvalidOperationException("The image could not be resized.")
            : null;

        using var image = SKImage.FromBitmap(resized ?? bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);

        return encoded.ToArray();
    }
}
