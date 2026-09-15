using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace MyApi.Tests.Infrastructure;

public sealed class ApiClient(HttpClient http)
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Task<HttpResponseMessage> GetAsync(string url) => http.GetAsync(url);

    public Task<HttpResponseMessage> PostAsync(string url, object? body = null) =>
        body is null ? http.PostAsync(url, null) : http.PostAsJsonAsync(url, body);

    public Task<HttpResponseMessage> PutAsync(string url, object body) => http.PutAsJsonAsync(url, body);

    public Task<HttpResponseMessage> DeleteAsync(string url) => http.DeleteAsync(url);

    public Task<HttpResponseMessage> UploadAsync(HttpMethod method, string url, byte[] content, string contentType)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent { { file, "file", "image" } };

        return http.SendAsync(new HttpRequestMessage(method, url) { Content = form });
    }

    public static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    public Task<HttpResponseMessage> LoginAsync(string email, string password = TestData.Password) =>
        PostAsync("/api/auth/login", new { email, password });

    public async Task<JsonNode> GetJsonAsync(string url)
    {
        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    public async Task<GraphQLResponse> GraphQLAsync(string query, object? variables = null)
    {
        using var response = await http.PostAsJsonAsync("/graphql", new { query, variables });
        var content = await response.Content.ReadAsStringAsync();

        if (JsonNode.Parse(content) is not JsonObject body)
        {
            throw new InvalidOperationException($"Unexpected GraphQL response ({(int)response.StatusCode}): {content}");
        }

        return new GraphQLResponse(body);
    }
}
