using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Auth;
using MyApi.Shared.Data;

namespace MyApi.Tests.Infrastructure;

public sealed record SellerAccount(ApiClient Client, Guid SellerId);

public sealed class Scenario(MyApiFactory factory)
{
    public const string CreateProductMutation =
        "mutation ($input: CreateProductDtoInput!) { createProduct(input: $input) { id } }";

    public const string AddToCartMutation =
        "mutation ($productId: UUID!, $quantity: Int!) { addToCart(input: { productId: $productId, quantity: $quantity }) { totalQuantity totalPrice } }";

    public const string CheckoutMutation =
        "mutation ($input: CheckoutDtoInput!) { checkout(input: $input) { id } }";

    public static object DeliveryAddressInput(string phone = "+1 (415) 555-0142", string city = "San Francisco") => new
    {
        recipientName = "Test Buyer",
        phone,
        country = "United States",
        city,
        addressLine = "500 Market Street",
        apartment = "12",
        postalCode = "94105",
        comment = (string?)null
    };

    public static object CheckoutInput(string phone = "+1 (415) 555-0142") =>
        new { deliveryAddress = DeliveryAddressInput(phone) };

    public static object ProductInput(Guid sellerId, Guid categoryId, int stock = 10, decimal price = 100m) => new
    {
        name = TestData.Name("product"),
        sku = TestData.Sku(),
        price,
        stock,
        categoryId,
        sellerId
    };

    public async Task<ApiClient> CustomerAsync()
    {
        var client = factory.CreateApiClient();
        client.Username = TestData.Name("customer");
        client.Email = TestData.Email("customer");

        var response = await client.PostAsync("/api/auth/register", new
        {
            username = client.Username,
            email = client.Email,
            password = TestData.Password
        });
        response.EnsureSuccessStatusCode();

        return client;
    }

    public async Task<SellerAccount> SellerAsync()
    {
        var client = factory.CreateApiClient();
        client.Username = TestData.Name("seller");
        client.Email = TestData.Email("seller");

        var response = await client.PostAsync("/api/auth/register-seller", new
        {
            username = client.Username,
            email = client.Email,
            password = TestData.Password,
            sellerName = TestData.Name("shop")
        });
        response.EnsureSuccessStatusCode();

        var shops = await client.GraphQLAsync("{ mySellers { seller { id } } }");

        return new SellerAccount(client, shops["mySellers"][0]!["seller"]!["id"].AsGuid());
    }

    public async Task<ApiClient> AdminAsync()
    {
        var client = factory.CreateApiClient();
        client.Username = TestData.Name("admin");
        client.Email = TestData.Email("admin");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var admin = new UserAccount(client.Username, client.Email, passwordHasher.Hash(TestData.Password));
            admin.ChangeRole(UserRole.Admin);

            dbContext.UserAccounts.Add(admin);
            await dbContext.SaveChangesAsync();
        }

        (await client.LoginAsync(client.Email)).EnsureSuccessStatusCode();

        return client;
    }

    public async Task<Guid> CategoryAsync()
    {
        var admin = await AdminAsync();
        var response = await admin.GraphQLAsync(
            "mutation ($name: String!) { createCategory(input: { name: $name }) { id } }",
            new { name = TestData.Name("category") });

        return response["createCategory"]["id"].AsGuid();
    }

    public async Task<Guid> ProductAsync(SellerAccount seller, int stock = 10, decimal price = 100m, Guid? categoryId = null)
    {
        var category = categoryId ?? await CategoryAsync();
        var response = await seller.Client.GraphQLAsync(
            CreateProductMutation,
            new { input = ProductInput(seller.SellerId, category, stock, price) });

        return response["createProduct"]["id"].AsGuid();
    }

    public async Task<JsonNode> ProductByIdAsync(Guid productId)
    {
        var response = await factory.CreateApiClient().GraphQLAsync(
            "query ($id: UUID!) { productById(id: $id) { stock isActive rating reviewCount } }",
            new { id = productId });

        return response["productById"];
    }

    public async Task<decimal> SellerRatingAsync(Guid sellerId)
    {
        var response = await factory.CreateApiClient().GraphQLAsync(
            "query ($id: UUID!) { sellerById(id: $id) { rating } }",
            new { id = sellerId });

        return response["sellerById"]["rating"].AsDecimal();
    }

    public async Task AddToCartAsync(ApiClient buyer, Guid productId, int quantity = 1) =>
        (await buyer.GraphQLAsync(AddToCartMutation, new { productId, quantity })).EnsureSuccess();

    public async Task<IReadOnlyList<Guid>> CheckoutAsync(ApiClient buyer)
    {
        var response = await buyer.GraphQLAsync(CheckoutMutation, new { input = CheckoutInput() });

        return response["checkout"].AsArray().Select(x => x!["id"].AsGuid()).ToList();
    }

    public async Task<Guid> DeliveredOrderAsync(ApiClient buyer, SellerAccount seller, Guid productId)
    {
        await AddToCartAsync(buyer, productId);
        var orderId = (await CheckoutAsync(buyer)).Single();

        (await seller.Client.GraphQLAsync(
            "mutation ($id: UUID!) { shipOrder(id: $id) { status } }",
            new { id = orderId })).EnsureSuccess();

        (await buyer.GraphQLAsync(
            "mutation ($id: UUID!) { confirmOrderDelivery(id: $id) { status } }",
            new { id = orderId })).EnsureSuccess();

        return orderId;
    }
}
