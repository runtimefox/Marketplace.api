using SkiaSharp;

namespace MyApi.Shared.Data;

public static class DemoImages
{
    public const int ProductVariants = 2;

    private const int ProductSize = 1200;
    private const int SquareSize = 512;

    public static byte[] Product(string name, string category, int variant)
    {
        var hue = (HueOf(name) + variant * 35) % 360;

        return Render(ProductSize, SKColor.FromHsl(hue, 55, 55), SKColor.FromHsl((hue + 40) % 360, 60, 28), canvas =>
        {
            DrawCentered(canvas, name, ProductSize * 0.47f, ProductSize * 0.075f, SKColors.White);
            DrawCentered(canvas, variant == 0 ? category : $"{category} · view {variant + 1}",
                ProductSize * 0.58f, ProductSize * 0.04f, SKColors.White.WithAlpha(210));
        });
    }

    public static byte[] Logo(string shopName) => Initials(shopName, 45, 40);

    public static byte[] Avatar(string username) => Initials(username, 50, 55);

    private static byte[] Initials(string value, float saturation, float lightness)
    {
        var hue = HueOf(value);

        return Render(SquareSize, SKColor.FromHsl(hue, saturation, lightness),
            SKColor.FromHsl((hue + 40) % 360, saturation, lightness - 20), canvas =>
                DrawCentered(canvas, InitialsOf(value), SquareSize / 2f, SquareSize * 0.4f, SKColors.White));
    }

    private static byte[] Render(int size, SKColor from, SKColor to, Action<SKCanvas> draw)
    {
        using var bitmap = new SKBitmap(size, size);

        using (var canvas = new SKCanvas(bitmap))
        using (var shader = SKShader.CreateLinearGradient(
                   new SKPoint(0, 0), new SKPoint(size, size), [from, to], SKShaderTileMode.Clamp))
        using (var background = new SKPaint { Shader = shader })
        {
            canvas.DrawRect(0, 0, size, size, background);
            draw(canvas);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private static void DrawCentered(SKCanvas canvas, string text, float centerY, float textSize, SKColor color)
    {
        var canvasWidth = canvas.DeviceClipBounds.Width;
        var maxWidth = canvasWidth * 0.85f;

        using var paint = new SKPaint { Color = color, IsAntialias = true };
        using var font = new SKFont { Size = textSize };

        var width = font.MeasureText(text, paint);
        if (width > maxWidth)
        {
            font.Size = textSize * maxWidth / width;
        }

        var metrics = font.Metrics;
        var baseline = centerY - (metrics.Ascent + metrics.Descent) / 2;

        canvas.DrawText(text, canvasWidth / 2f, baseline, SKTextAlign.Center, font, paint);
    }

    private static float HueOf(string value)
    {
        var hash = 17;
        foreach (var character in value)
        {
            hash = (hash * 31 + character) % 360;
        }

        return hash;
    }

    private static string InitialsOf(string value)
    {
        var words = value.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);

        var initials = words.Length >= 2
            ? $"{words[0][0]}{words[1][0]}"
            : value[..Math.Min(2, value.Length)];

        return initials.ToUpperInvariant();
    }
}
