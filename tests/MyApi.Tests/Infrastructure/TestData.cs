namespace MyApi.Tests.Infrastructure;

public static class TestData
{
    public const string Password = "Password123";

    public static string Name(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public static string Email(string prefix) => $"{prefix}.{Guid.NewGuid():N}@test.dev";

    public static string Sku() => $"TEST-{Guid.NewGuid():N}";
}
