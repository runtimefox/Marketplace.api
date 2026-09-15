using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ForwardedHeadersTests(MyApiFactory factory)
{
    [Fact]
    public async Task RateLimit_UsesClientIpForwardedByTrustedProxy()
    {
        await using var app = CreateLimitedApp();

        var firstClient = new List<int>();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            firstClient.Add(await LoginAsync(app, IPAddress.Loopback, "203.0.113.10"));
        }

        var secondClient = await LoginAsync(app, IPAddress.Loopback, "203.0.113.20");

        Assert.Equal(new[] { 401, 401, 429 }, firstClient);
        Assert.Equal(401, secondClient);
    }

    [Fact]
    public async Task ForwardedFor_FromUntrustedSource_IsIgnored()
    {
        await using var app = CreateLimitedApp();
        var untrustedSource = IPAddress.Parse("198.51.100.7");

        var statuses = new List<int>();
        foreach (var spoofedClient in new[] { "203.0.113.30", "203.0.113.31", "203.0.113.32" })
        {
            statuses.Add(await LoginAsync(app, untrustedSource, spoofedClient));
        }

        Assert.Equal(new[] { 401, 401, 429 }, statuses);
    }

    private WebApplicationFactory<Program> CreateLimitedApp() =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["RateLimiting:Auth:PermitLimit"] = "2" })));

    private static async Task<int> LoginAsync(
        WebApplicationFactory<Program> app, IPAddress remoteAddress, string forwardedFor)
    {
        var body = Encoding.UTF8.GetBytes("""{"email":"nobody@test.dev","password":"WrongPassword1"}""");

        var context = await app.Server.SendAsync(httpContext =>
        {
            httpContext.Request.Method = HttpMethods.Post;
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");
            httpContext.Request.Path = "/api/auth/login";
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.ContentLength = body.Length;
            httpContext.Request.Body = new MemoryStream(body);
            httpContext.Request.Headers["X-Forwarded-For"] = forwardedFor;
            httpContext.Connection.RemoteIpAddress = remoteAddress;
        });

        return context.Response.StatusCode;
    }
}
