using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ReviewTests(MyApiFactory factory)
{
    private const string CreateReview =
        "mutation ($productId: UUID!, $rating: Int!) { createReview(input: { productId: $productId, rating: $rating }) { id rating } }";

    private const string UpdateReview =
        "mutation ($id: UUID!, $rating: Int!) { updateReview(id: $id, input: { rating: $rating }) { rating } }";

    private const string DeleteReview = "mutation ($id: UUID!) { deleteReview(id: $id) }";

    private const string CanReview = "query ($productId: UUID!) { canReviewProduct(productId: $productId) }";

    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Review_WithoutDeliveredOrder_IsForbidden()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.AddToCartAsync(buyer, productId);
        await _scenario.CheckoutAsync(buyer);

        var canReview = await buyer.GraphQLAsync(CanReview, new { productId });
        var review = await buyer.GraphQLAsync(CreateReview, new { productId, rating = 5 });

        Assert.False(canReview["canReviewProduct"].AsBool());
        review.AssertError("FORBIDDEN");
    }

    [Fact]
    public async Task ShopMember_CannotReviewOwnProduct()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);

        var review = await seller.Client.GraphQLAsync(CreateReview, new { productId, rating = 5 });

        review.AssertError("FORBIDDEN");
    }

    [Fact]
    public async Task DeliveredOrder_AllowsSingleReview_AndUpdatesRatings()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.DeliveredOrderAsync(buyer, seller, productId);

        var canReviewBefore = await buyer.GraphQLAsync(CanReview, new { productId });
        var review = await buyer.GraphQLAsync(CreateReview, new { productId, rating = 5 });
        var duplicate = await buyer.GraphQLAsync(CreateReview, new { productId, rating = 4 });
        var canReviewAfter = await buyer.GraphQLAsync(CanReview, new { productId });
        var product = await _scenario.ProductByIdAsync(productId);

        Assert.True(canReviewBefore["canReviewProduct"].AsBool());
        Assert.Equal(5, review["createReview"]["rating"].AsInt());
        duplicate.AssertError("INVALID_INPUT");
        Assert.False(canReviewAfter["canReviewProduct"].AsBool());
        Assert.Equal(5m, product["rating"].AsDecimal());
        Assert.Equal(1, product["reviewCount"].AsInt());
        Assert.Equal(5m, await _scenario.SellerRatingAsync(seller.SellerId));
    }

    [Fact]
    public async Task SellerRating_IsAverageOfReviews_AndFollowsUpdatesAndDeletes()
    {
        var seller = await _scenario.SellerAsync();
        var firstProduct = await _scenario.ProductAsync(seller);
        var secondProduct = await _scenario.ProductAsync(seller);
        var firstBuyer = await _scenario.CustomerAsync();
        var secondBuyer = await _scenario.CustomerAsync();
        var admin = await _scenario.AdminAsync();
        await _scenario.DeliveredOrderAsync(firstBuyer, seller, firstProduct);
        await _scenario.DeliveredOrderAsync(secondBuyer, seller, secondProduct);

        var firstReview = (await firstBuyer.GraphQLAsync(CreateReview, new { productId = firstProduct, rating = 5 }))["createReview"]["id"].AsGuid();
        var afterFirst = await _scenario.SellerRatingAsync(seller.SellerId);

        var secondReview = (await secondBuyer.GraphQLAsync(CreateReview, new { productId = secondProduct, rating = 2 }))["createReview"]["id"].AsGuid();
        var afterSecond = await _scenario.SellerRatingAsync(seller.SellerId);

        (await secondBuyer.GraphQLAsync(UpdateReview, new { id = secondReview, rating = 4 })).EnsureSuccess();
        var afterUpdate = await _scenario.SellerRatingAsync(seller.SellerId);

        (await admin.GraphQLAsync(DeleteReview, new { id = firstReview })).EnsureSuccess();
        var afterAdminDelete = await _scenario.SellerRatingAsync(seller.SellerId);

        (await secondBuyer.GraphQLAsync(DeleteReview, new { id = secondReview })).EnsureSuccess();
        var afterAllDeleted = await _scenario.SellerRatingAsync(seller.SellerId);

        Assert.Equal(5m, afterFirst);
        Assert.Equal(3.5m, afterSecond);
        Assert.Equal(4.5m, afterUpdate);
        Assert.Equal(4m, afterAdminDelete);
        Assert.Equal(0m, afterAllDeleted);
    }

    [Fact]
    public async Task OnlyAuthorOrAdmin_CanChangeReview()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        var stranger = await _scenario.CustomerAsync();
        await _scenario.DeliveredOrderAsync(buyer, seller, productId);
        var id = (await buyer.GraphQLAsync(CreateReview, new { productId, rating = 5 }))["createReview"]["id"].AsGuid();

        var strangerUpdates = await stranger.GraphQLAsync(UpdateReview, new { id, rating = 1 });
        var sellerDeletes = await seller.Client.GraphQLAsync(DeleteReview, new { id });

        strangerUpdates.AssertError("FORBIDDEN");
        sellerDeletes.AssertError("FORBIDDEN");
    }
}
