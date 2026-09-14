using Microsoft.AspNetCore.Mvc.Testing;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class SchemaSnapshotTests(MyApiFactory factory)
{
    private const string UpdateHint =
        "Regenerate it with: UPDATE_SCHEMA=1 dotnet test tests/MyApi.Tests --filter SchemaSnapshotTests";

    [Fact]
    public async Task SchemaFile_MatchesCurrentSchema()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var schema = Normalize(await client.GetStringAsync("/graphql?sdl"));
        var path = Path.Combine(FindRepositoryRoot(), "schema.graphql");

        if (Environment.GetEnvironmentVariable("UPDATE_SCHEMA") == "1")
        {
            await File.WriteAllTextAsync(path, schema);
        }

        Assert.True(File.Exists(path), $"schema.graphql is missing. {UpdateHint}");
        Assert.True(
            Normalize(await File.ReadAllTextAsync(path)) == schema,
            $"schema.graphql is outdated. {UpdateHint}");
    }

    private static string Normalize(string sdl) => sdl.Replace("\r\n", "\n").TrimEnd() + "\n";

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MyApi.csproj")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root with MyApi.csproj was not found.");
    }
}
