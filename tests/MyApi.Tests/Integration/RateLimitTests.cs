using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class RateLimitTests(MyApiFactory factory)
{
    [Fact]
    public async Task Login_AboveLimit_ReturnsTooManyRequests()
    {
        await using var limitedApp = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["RateLimiting:Auth:PermitLimit"] = "2" })));

        using var client = limitedApp.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });

        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage? lastResponse = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            lastResponse?.Dispose();
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = TestData.Email("nobody"),
                password = "WrongPassword1"
            });
            statuses.Add(lastResponse.StatusCode);
        }

        Assert.Equal(
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests },
            statuses);
        Assert.True(lastResponse!.Headers.Contains("Retry-After"));
        lastResponse.Dispose();
    }
}
