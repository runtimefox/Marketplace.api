using System.Net;
using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class UserAdminTests(MyApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task UsersList_ForCustomer_IsForbidden()
    {
        var customer = await _scenario.CustomerAsync();

        var rest = await customer.GetAsync("/api/user");
        var graphQL = await customer.GraphQLAsync("{ users { email } }");

        Assert.Equal(HttpStatusCode.Forbidden, rest.StatusCode);
        graphQL.AssertError("AUTH_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task UsersList_ForAdmin_IsReturned()
    {
        var admin = await _scenario.AdminAsync();

        var rest = await admin.GetAsync("/api/user");
        var graphQL = await admin.GraphQLAsync("{ users { email role } }");

        Assert.Equal(HttpStatusCode.OK, rest.StatusCode);
        Assert.Contains(
            graphQL["users"].AsArray(),
            x => x!["email"].AsString() == admin.Email && x["role"].AsString() == "ADMIN");
    }
}
