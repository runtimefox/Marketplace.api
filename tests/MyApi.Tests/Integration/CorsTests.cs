using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class CorsTests(MyApiFactory factory)
{
    [Fact]
    public async Task Preflight_FromAllowedOrigin_AllowsCredentials()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/graphql");
        request.Headers.Add("Origin", MyApiFactory.FrontendOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(MyApiFactory.FrontendOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task Request_FromUnknownOrigin_GetsNoCorsHeaders()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new { query = "{ __typename }" })
        };
        request.Headers.Add("Origin", "https://unknown.example");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });
}
