using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApi.Shared.Data;
using Testcontainers.PostgreSql;

namespace MyApi.Tests.Infrastructure;

public sealed class MyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string EnvironmentName = "Testing";
    private const string JwtKey = "integration-tests-jwt-signing-key-0123456789";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17").Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentName);
        Environment.SetEnvironmentVariable("ConnectionStrings__DbConnection", _database.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public ApiClient CreateApiClient() =>
        new(CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        }));

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment(EnvironmentName);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
