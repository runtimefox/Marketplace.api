using MyApi.Models.Entities;

namespace MyApi.Tests.Unit;

public class ReviewReplyEntityTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankText_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => new ReviewReply(Guid.NewGuid(), Guid.NewGuid(), false, text));
    }

    [Fact]
    public void Constructor_WithTooLongText_Throws()
    {
        var text = new string('a', ReviewReply.TextMaxLength + 1);

        Assert.Throws<ArgumentException>(() => new ReviewReply(Guid.NewGuid(), Guid.NewGuid(), true, text));
    }

    [Fact]
    public void Edit_TrimsText_AndKeepsAuthorAndSellerFlag()
    {
        var userId = Guid.NewGuid();
        var reply = new ReviewReply(Guid.NewGuid(), userId, true, "  Thanks for the review  ");
        var original = reply.Text;

        reply.Edit(" Glad you like it ");

        Assert.Equal("Thanks for the review", original);
        Assert.Equal("Glad you like it", reply.Text);
        Assert.Equal(userId, reply.UserAccountId);
        Assert.True(reply.IsFromSeller);
        Assert.True(reply.UpdatedAt >= reply.CreatedAt);
    }
}
