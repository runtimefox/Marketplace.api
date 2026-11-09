using MyApi.Tests.Infrastructure;

namespace MyApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ReviewReplyTests(MyApiFactory factory)
{
    private const string CreateReview =
        "mutation ($productId: UUID!) { createReview(input: { productId: $productId, rating: 4 }) { id } }";

    private const string DeleteReview = "mutation ($id: UUID!) { deleteReview(id: $id) }";

    private const string CreateReply =
        "mutation ($reviewId: UUID!, $text: String!) { createReviewReply(input: { reviewId: $reviewId, text: $text }) { id text isFromSeller author { id } } }";

    private const string UpdateReply =
        "mutation ($id: UUID!, $text: String!) { updateReviewReply(id: $id, input: { text: $text }) { text } }";

    private const string DeleteReply = "mutation ($id: UUID!) { deleteReviewReply(id: $id) }";

    private const string ProductReviews =
        "query ($productId: UUID!) { productReviews(productId: $productId, first: 10) { nodes { id replies { text isFromSeller } } } }";

    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task AuthorAndShop_CanReply_AndRepliesAreReturnedInOrder()
    {
        var (seller, buyer, productId, reviewId) = await ReviewedProductAsync();

        var sellerReply = await seller.Client.GraphQLAsync(CreateReply, new { reviewId, text = "  Thank you!  " });
        var buyerReply = await buyer.GraphQLAsync(CreateReply, new { reviewId, text = "You are welcome" });
        var reviews = await buyer.GraphQLAsync(ProductReviews, new { productId });

        Assert.Equal("Thank you!", sellerReply["createReviewReply"]["text"].AsString());
        Assert.True(sellerReply["createReviewReply"]["isFromSeller"].AsBool());
        Assert.False(buyerReply["createReviewReply"]["isFromSeller"].AsBool());

        var replies = reviews["productReviews"]["nodes"]![0]!["replies"]!.AsArray();
        Assert.Equal(2, replies.Count);
        Assert.Equal("Thank you!", replies[0]!["text"].AsString());
        Assert.True(replies[0]!["isFromSeller"].AsBool());
        Assert.Equal("You are welcome", replies[1]!["text"].AsString());
    }

    [Fact]
    public async Task Stranger_CannotReply_AndMissingReviewIsInvalid()
    {
        var (_, _, _, reviewId) = await ReviewedProductAsync();
        var stranger = await _scenario.CustomerAsync();

        var strangerReply = await stranger.GraphQLAsync(CreateReply, new { reviewId, text = "Hi" });
        var missingReview = await stranger.GraphQLAsync(CreateReply, new { reviewId = Guid.NewGuid(), text = "Hi" });

        strangerReply.AssertError("FORBIDDEN");
        missingReview.AssertError("INVALID_INPUT");
    }

    [Fact]
    public async Task BlankReply_IsInvalid()
    {
        var (_, buyer, _, reviewId) = await ReviewedProductAsync();

        var reply = await buyer.GraphQLAsync(CreateReply, new { reviewId, text = "   " });

        reply.AssertError("INVALID_INPUT");
    }

    [Fact]
    public async Task OnlyAuthorEdits_AndAuthorOrAdminDeletes()
    {
        var (seller, buyer, _, reviewId) = await ReviewedProductAsync();
        var admin = await _scenario.AdminAsync();
        var sellerReplyId = (await seller.Client.GraphQLAsync(CreateReply, new { reviewId, text = "Thanks" }))
            ["createReviewReply"]["id"].AsGuid();
        var buyerReplyId = (await buyer.GraphQLAsync(CreateReply, new { reviewId, text = "Welcome" }))
            ["createReviewReply"]["id"].AsGuid();

        var buyerEditsSeller = await buyer.GraphQLAsync(UpdateReply, new { id = sellerReplyId, text = "Hacked" });
        var sellerEdits = await seller.Client.GraphQLAsync(UpdateReply, new { id = sellerReplyId, text = "Thanks again" });
        var sellerDeletesBuyer = await seller.Client.GraphQLAsync(DeleteReply, new { id = buyerReplyId });
        var buyerDeletes = await buyer.GraphQLAsync(DeleteReply, new { id = buyerReplyId });
        var adminDeletes = await admin.GraphQLAsync(DeleteReply, new { id = sellerReplyId });
        var deleteAgain = await admin.GraphQLAsync(DeleteReply, new { id = sellerReplyId });

        buyerEditsSeller.AssertError("FORBIDDEN");
        Assert.Equal("Thanks again", sellerEdits["updateReviewReply"]["text"].AsString());
        sellerDeletesBuyer.AssertError("FORBIDDEN");
        Assert.True(buyerDeletes["deleteReviewReply"].AsBool());
        Assert.True(adminDeletes["deleteReviewReply"].AsBool());
        Assert.False(deleteAgain["deleteReviewReply"].AsBool());
    }

    [Fact]
    public async Task DeletingReview_RemovesItsReplies()
    {
        var (seller, buyer, productId, reviewId) = await ReviewedProductAsync();
        (await seller.Client.GraphQLAsync(CreateReply, new { reviewId, text = "Thanks" })).EnsureSuccess();

        var deleted = await buyer.GraphQLAsync(DeleteReview, new { id = reviewId });
        var reviews = await buyer.GraphQLAsync(ProductReviews, new { productId });

        Assert.True(deleted["deleteReview"].AsBool());
        Assert.Empty(reviews["productReviews"]["nodes"]!.AsArray());
    }

    private async Task<(SellerAccount Seller, ApiClient Buyer, Guid ProductId, Guid ReviewId)> ReviewedProductAsync()
    {
        var seller = await _scenario.SellerAsync();
        var productId = await _scenario.ProductAsync(seller);
        var buyer = await _scenario.CustomerAsync();
        await _scenario.DeliveredOrderAsync(buyer, seller, productId);
        var reviewId = (await buyer.GraphQLAsync(CreateReview, new { productId }))["createReview"]["id"].AsGuid();

        return (seller, buyer, productId, reviewId);
    }
}
