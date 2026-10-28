using System.Net;
using System.Text.Json.Nodes;
using MyApi.Tests.Infrastructure;
using SkiaSharp;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ImageTests(MyApiFactory factory)
{
    private const string AddManager =
        "mutation ($sellerId: UUID!, $email: String!) { addSellerManager(sellerId: $sellerId, input: { email: $email }) { userId } }";

    private static readonly HttpClient Storage = new();

    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task ProductImages_AreResizedOrderedReorderedAndDeleted()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var imagesUrl = $"/api/products/{productId}/images";

        var first = await ExpectJsonAsync(
            await seller.Client.UploadAsync(HttpMethod.Post, imagesUrl, TestImages.Create(1600, 1200), "image/png"),
            HttpStatusCode.Created);
        var second = await ExpectJsonAsync(
            await seller.Client.UploadAsync(
                HttpMethod.Post, imagesUrl, TestImages.Create(400, 300, SKEncodedImageFormat.Jpeg), "image/jpeg"),
            HttpStatusCode.Created);

        Assert.Equal(0, first["position"].AsInt());
        Assert.Equal(1, second["position"].AsInt());
        Assert.Equal((320, 240, SKEncodedImageFormat.Webp), await DescribeAsync(first["smallUrl"].AsString()));
        Assert.Equal((1280, 960, SKEncodedImageFormat.Webp), await DescribeAsync(first["largeUrl"].AsString()));
        Assert.Equal((400, 300, SKEncodedImageFormat.Webp), await DescribeAsync(second["largeUrl"].AsString()));

        var firstId = first["id"].AsGuid();
        var secondId = second["id"].AsGuid();

        var reordered = await seller.Client.PutAsync($"{imagesUrl}/order", new { imageIds = new[] { secondId, firstId } });
        Assert.Equal(HttpStatusCode.OK, reordered.StatusCode);

        var product = await factory.CreateApiClient().GraphQLAsync(
            "query ($id: UUID!) { productById(id: $id) { images { id position smallUrl } mainImage { smallUrl } } }",
            new { id = productId });

        var images = product["productById"]["images"]!.AsArray();
        Assert.Equal(new[] { secondId, firstId }, images.Select(x => x!["id"].AsGuid()));
        Assert.Equal(second["smallUrl"].AsString(), product["productById"]["mainImage"]!["smallUrl"].AsString());

        var deleted = await seller.Client.DeleteAsync($"{imagesUrl}/{secondId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False((await Storage.GetAsync(second["smallUrl"].AsString())).IsSuccessStatusCode);

        var afterDelete = await factory.CreateApiClient().GraphQLAsync(
            "query ($id: UUID!) { productById(id: $id) { images { id position } } }",
            new { id = productId });

        var remaining = Assert.Single(afterDelete["productById"]["images"]!.AsArray())!;
        Assert.Equal(firstId, remaining["id"].AsGuid());
        Assert.Equal(0, remaining["position"].AsInt());
    }

    [Fact]
    public async Task ProductImages_ForNonMember_AreForbidden()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var customer = await _scenario.CustomerAsync();

        var response = await customer.UploadAsync(
            HttpMethod.Post, $"/api/products/{productId}/images", TestImages.Create(100, 100), "image/png");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("image/png", "not an image at all")]
    [InlineData("text/plain", "plain text")]
    public async Task ProductImages_WithInvalidFile_AreRejected(string contentType, string content)
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);

        var response = await seller.Client.UploadAsync(
            HttpMethod.Post, $"/api/products/{productId}/images", System.Text.Encoding.UTF8.GetBytes(content), contentType);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProductImages_AboveLimit_AreRejected()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var imagesUrl = $"/api/products/{productId}/images";

        for (var index = 0; index < 10; index++)
        {
            var uploaded = await seller.Client.UploadAsync(HttpMethod.Post, imagesUrl, TestImages.Create(20, 20), "image/png");
            Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        }

        var eleventh = await seller.Client.UploadAsync(HttpMethod.Post, imagesUrl, TestImages.Create(20, 20), "image/png");

        Assert.Equal(HttpStatusCode.BadRequest, eleventh.StatusCode);
    }

    [Fact]
    public async Task Avatar_CanBeUploadedReplacedAndDeleted()
    {
        var customer = await _scenario.CustomerAsync();

        var first = await ExpectJsonAsync(
            await customer.UploadAsync(HttpMethod.Put, "/api/auth/me/avatar", TestImages.Create(600, 600), "image/png"),
            HttpStatusCode.OK);
        var second = await ExpectJsonAsync(
            await customer.UploadAsync(HttpMethod.Put, "/api/auth/me/avatar", TestImages.Create(300, 300), "image/png"),
            HttpStatusCode.OK);
        var me = await customer.GraphQLAsync("{ me { username avatar { smallUrl } } }");

        Assert.Equal((96, 96, SKEncodedImageFormat.Webp), await DescribeAsync(second["smallUrl"].AsString()));
        Assert.Equal(second["smallUrl"].AsString(), me["me"]["avatar"]!["smallUrl"].AsString());
        Assert.False((await Storage.GetAsync(first["smallUrl"].AsString())).IsSuccessStatusCode);

        var deleted = await customer.DeleteAsync("/api/auth/me/avatar");
        var deletedAgain = await customer.DeleteAsync("/api/auth/me/avatar");
        var meAfterDelete = await customer.GraphQLAsync("{ me { avatar { smallUrl } } }");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deletedAgain.StatusCode);
        Assert.Null(meAfterDelete["me"]["avatar"]);
    }

    [Fact]
    public async Task SellerLogo_IsOwnerOnly_AndShownOnShop()
    {
        var owner = await _scenario.SellerAsync();
        var manager = await _scenario.CustomerAsync();
        (await owner.Client.GraphQLAsync(AddManager, new { sellerId = owner.SellerId, email = manager.Email })).EnsureSuccess();
        var logoUrl = $"/api/sellers/{owner.SellerId}/logo";

        var managerUpload = await manager.UploadAsync(HttpMethod.Put, logoUrl, TestImages.Create(400, 400), "image/png");
        var ownerUpload = await ExpectJsonAsync(
            await owner.Client.UploadAsync(HttpMethod.Put, logoUrl, TestImages.Create(400, 400), "image/png"),
            HttpStatusCode.OK);
        var shop = await factory.CreateApiClient().GraphQLAsync(
            "query ($id: UUID!) { sellerById(id: $id) { logo { smallUrl largeUrl } } }",
            new { id = owner.SellerId });

        Assert.Equal(HttpStatusCode.Forbidden, managerUpload.StatusCode);
        Assert.Equal(ownerUpload["smallUrl"].AsString(), shop["sellerById"]["logo"]!["smallUrl"].AsString());
        Assert.Equal((128, 128, SKEncodedImageFormat.Webp), await DescribeAsync(ownerUpload["smallUrl"].AsString()));
    }

    [Fact]
    public async Task Upload_WithoutSignIn_ReturnsUnauthorized()
    {
        var response = await factory.CreateApiClient().UploadAsync(
            HttpMethod.Put, "/api/auth/me/avatar", TestImages.Create(100, 100), "image/png");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<JsonNode> ExpectJsonAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {body}");

        return JsonNode.Parse(body)!;
    }

    private static async Task<(int Width, int Height, SKEncodedImageFormat Format)> DescribeAsync(string url) =>
        TestImages.Describe(await Storage.GetByteArrayAsync(url));
}
