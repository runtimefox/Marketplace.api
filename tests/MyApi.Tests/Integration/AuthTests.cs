using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthTests(MyApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Register_SignsInAsCustomer()
    {
        var customer = await _scenario.CustomerAsync();

        var me = await customer.GetJsonAsync("/api/auth/me");

        Assert.Equal(customer.Username, me["username"].AsString());
        Assert.Equal("Customer", me["role"].AsString());
    }

    [Fact]
    public async Task Me_WithoutSignIn_ReturnsUnauthorized()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var customer = await _scenario.CustomerAsync();

        var response = await factory.CreateApiClient().LoginAsync(customer.Email, "WrongPassword1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReusedRefreshToken_RevokesAllSessions()
    {
        var client = factory.CreateApiClient();
        var register = await client.PostAsync("/api/auth/register", new
        {
            username = TestData.Name("customer"),
            email = TestData.Email("customer"),
            password = TestData.Password
        });
        register.EnsureSuccessStatusCode();
        var oldRefreshToken = ReadCookie(register, "refresh_token");

        var rotated = await client.PostAsync("/api/auth/refresh");
        var reused = await RefreshWithTokenAsync(oldRefreshToken);
        var afterReuse = await client.PostAsync("/api/auth/refresh");

        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_SignsOutOtherSessions_AndKeepsCurrentOne()
    {
        var current = await _scenario.CustomerAsync();
        var other = factory.CreateApiClient();
        (await other.LoginAsync(current.Email)).EnsureSuccessStatusCode();

        var change = await current.PutAsync("/api/auth/me/password", new
        {
            currentPassword = TestData.Password,
            newPassword = "NewPassword456"
        });

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.PostAsync("/api/auth/refresh")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await current.PostAsync("/api/auth/refresh")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateApiClient().LoginAsync(current.Email)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateApiClient().LoginAsync(current.Email, "NewPassword456")).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        var customer = await _scenario.CustomerAsync();

        var response = await customer.PutAsync("/api/auth/me/password", new
        {
            currentPassword = "WrongPassword1",
            newPassword = "NewPassword456"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithTakenUsername_ReturnsConflict()
    {
        var first = await _scenario.CustomerAsync();
        var second = await _scenario.CustomerAsync();

        var response = await second.PutAsync("/api/auth/me", new { username = first.Username, email = second.Email });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<HttpResponseMessage> RefreshWithTokenAsync(string refreshToken)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");

        return await client.SendAsync(request);
    }

    private static string ReadCookie(HttpResponseMessage response, string name) =>
        response.Headers.GetValues("Set-Cookie")
            .Select(x => x.Split(';')[0])
            .First(x => x.StartsWith($"{name}="))[(name.Length + 1)..];
}
