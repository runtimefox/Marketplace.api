using MyApi.Models.Entities;

namespace MyApi.Tests.Unit;

public class ReviewEntityTests
{
    [Theory]
    [InlineData(Review.MinRating - 1)]
    [InlineData(Review.MaxRating + 1)]
    public void Constructor_WithRatingOutOfRange_Throws(int rating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Review(Guid.NewGuid(), Guid.NewGuid(), rating, null));
    }

    [Fact]
    public void Update_TrimsComment_AndTurnsBlankIntoNull()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 5, "  Great phone  ");
        var trimmed = review.Comment;

        review.Update(4, "   ");

        Assert.Equal("Great phone", trimmed);
        Assert.Null(review.Comment);
        Assert.Equal(4, review.Rating);
    }
}
