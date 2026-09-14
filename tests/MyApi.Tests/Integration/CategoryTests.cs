using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class CategoryTests(MyApiFactory factory)
{
    private const string CreateCategory =
        "mutation ($name: String!) { createCategory(input: { name: $name }) { id name } }";

    private const string DeleteCategory = "mutation ($id: UUID!) { deleteCategory(id: $id) }";

    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Admin_CanCreateRenameAndDeleteCategory()
    {
        var admin = await _scenario.AdminAsync();
        var name = TestData.Name("category");

        var created = await admin.GraphQLAsync(CreateCategory, new { name });
        var id = created["createCategory"]["id"].AsGuid();

        var renamed = await admin.GraphQLAsync(
            "mutation ($id: UUID!, $name: String!) { updateCategory(id: $id, input: { name: $name }) { name } }",
            new { id, name = $"{name}_renamed" });

        var deleted = await admin.GraphQLAsync(DeleteCategory, new { id });

        var found = await factory.CreateApiClient().GraphQLAsync(
            "query ($id: UUID!) { categoryById(id: $id) { id } }",
            new { id });

        Assert.Equal($"{name}_renamed", renamed["updateCategory"]["name"].AsString());
        Assert.True(deleted["deleteCategory"].AsBool());
        Assert.Null(found.EnsureSuccess().Data!["categoryById"]);
    }

    [Fact]
    public async Task Customer_CannotCreateCategory()
    {
        var customer = await _scenario.CustomerAsync();

        var response = await customer.GraphQLAsync(CreateCategory, new { name = TestData.Name("category") });

        response.AssertError("AUTH_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task DuplicateName_IsRejected()
    {
        var admin = await _scenario.AdminAsync();
        var name = TestData.Name("category");
        (await admin.GraphQLAsync(CreateCategory, new { name })).EnsureSuccess();

        var duplicate = await admin.GraphQLAsync(CreateCategory, new { name });

        duplicate.AssertError("INVALID_INPUT");
    }

    [Fact]
    public async Task CategoryWithProducts_CannotBeDeleted()
    {
        var seller = await _scenario.SellerAsync();
        var categoryId = await _scenario.CategoryAsync();
        await _scenario.ProductAsync(seller, categoryId: categoryId);
        var admin = await _scenario.AdminAsync();

        var response = await admin.GraphQLAsync(DeleteCategory, new { id = categoryId });

        response.AssertError("INVALID_INPUT");
    }
}
