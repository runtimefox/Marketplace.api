namespace MyApi.Services.Interfaces.Images;

public static class ImageVariants
{
    public const string SmallFileName = "small.webp";
    public const string LargeFileName = "large.webp";
    public const string ContentType = "image/webp";

    public static (int Small, int Large) MaxSides(ImageKind kind) => kind switch
    {
        ImageKind.ProductPhoto => (320, 1280),
        ImageKind.SellerLogo => (128, 512),
        ImageKind.UserAvatar => (96, 512),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown image kind.")
    };

    public static string SmallKey(string storageKey) => $"{storageKey}/{SmallFileName}";

    public static string LargeKey(string storageKey) => $"{storageKey}/{LargeFileName}";
}
