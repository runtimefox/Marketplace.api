using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApi.Shared.Data;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace MyApi.Tests.Infrastructure;

public sealed class MyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string FrontendOrigin = "http://localhost:5173";

    private const string EnvironmentName = "Testing";
    private const string JwtKey = "integration-tests-jwt-signing-key-0123456789";
    private const string StorageBucket = "test-images";
    private const string StorageRegion = "us-east-1";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17").Build();

    private readonly MinioContainer _storage =
        new MinioBuilder("quay.io/minio/minio:RELEASE.2025-09-07T16-13-09Z").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _storage.StartAsync());

        var storageUrl = _storage.GetConnectionString().TrimEnd('/');

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentName);
        Environment.SetEnvironmentVariable("ConnectionStrings__DbConnection", _database.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Storage__ServiceUrl", storageUrl);
        Environment.SetEnvironmentVariable("Storage__PublicBaseUrl", $"{storageUrl}/{StorageBucket}");
        Environment.SetEnvironmentVariable("Storage__BucketName", StorageBucket);
        Environment.SetEnvironmentVariable("Storage__AccessKey", _storage.GetAccessKey());
        Environment.SetEnvironmentVariable("Storage__SecretKey", _storage.GetSecretKey());
        Environment.SetEnvironmentVariable("Storage__Region", StorageRegion);

        await CreatePublicBucketAsync(storageUrl);

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public ApiClient CreateApiClient() =>
        new(CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        }));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Frontend:AllowedOrigins:0"] = FrontendOrigin,
                ["RateLimiting:Auth:PermitLimit"] = "100000"
            }));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        await _storage.DisposeAsync();
    }

    private async Task CreatePublicBucketAsync(string storageUrl)
    {
        using var s3 = new AmazonS3Client(
            new BasicAWSCredentials(_storage.GetAccessKey(), _storage.GetSecretKey()),
            new AmazonS3Config
            {
                ServiceURL = storageUrl,
                AuthenticationRegion = StorageRegion,
                ForcePathStyle = true,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });

        await s3.PutBucketAsync(new PutBucketRequest { BucketName = StorageBucket });
        await s3.PutBucketPolicyAsync(new PutBucketPolicyRequest
        {
            BucketName = StorageBucket,
            Policy = $$"""
                {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{StorageBucket}}/*"]}]}
                """
        });
    }
}
