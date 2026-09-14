using System.Text.Json.Nodes;

namespace MyApi.Tests.Infrastructure;

public sealed class GraphQLResponse(JsonObject body)
{
    public JsonNode? Data => body["data"];

    public JsonArray? Errors => body["errors"] as JsonArray;

    public string? ErrorCode => Errors?[0]?["extensions"]?["code"]?.GetValue<string>();

    public JsonNode this[string field] =>
        EnsureSuccess().Data?[field] ?? throw new InvalidOperationException($"Field {field} is null or missing.");

    public GraphQLResponse EnsureSuccess()
    {
        if (Errors is { Count: > 0 })
        {
            Assert.Fail($"Unexpected GraphQL errors: {Errors.ToJsonString()}");
        }

        return this;
    }

    public void AssertError(string expectedCode)
    {
        Assert.True(Errors is { Count: > 0 }, $"Expected GraphQL error {expectedCode}, got: {body.ToJsonString()}");
        Assert.Equal(expectedCode, ErrorCode);
    }
}

public static class JsonNodeExtensions
{
    public static Guid AsGuid(this JsonNode? node) => Guid.Parse(node!.GetValue<string>());

    public static string AsString(this JsonNode? node) => node!.GetValue<string>();

    public static decimal AsDecimal(this JsonNode? node) => node!.GetValue<decimal>();

    public static int AsInt(this JsonNode? node) => node!.GetValue<int>();

    public static bool AsBool(this JsonNode? node) => node!.GetValue<bool>();
}
