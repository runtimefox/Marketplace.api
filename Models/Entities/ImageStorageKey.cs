namespace MyApi.Models.Entities;

public static class ImageStorageKey
{
    public const int MaxLength = 200;

    public static string Validate(string storageKey, string paramName)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key must not be empty.", paramName);
        }

        if (storageKey.Length > MaxLength)
        {
            throw new ArgumentException($"Length must not exceed {MaxLength} characters.", paramName);
        }

        return storageKey;
    }
}
