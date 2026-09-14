using HotChocolate;
using MyApi.GraphQL;

namespace MyApi.Tests.Unit;

public class DomainErrorFilterTests
{
    private const string UnexpectedError = "Unexpected Execution Error";

    private readonly DomainErrorFilter _filter = new();

    [Fact]
    public void ArgumentException_IsExposedWithoutParameterName()
    {
        var result = _filter.OnError(ErrorWith(
            new ArgumentOutOfRangeException("rating", "Rating must be between 1 and 5.")));

        Assert.Equal("Rating must be between 1 and 5.", result.Message);
        Assert.Equal("INVALID_INPUT", result.Code);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void UnauthorizedAccessException_IsExposedAsForbidden()
    {
        var result = _filter.OnError(ErrorWith(new UnauthorizedAccessException("You are not a member of this shop.")));

        Assert.Equal("You are not a member of this shop.", result.Message);
        Assert.Equal("FORBIDDEN", result.Code);
    }

    [Fact]
    public void UnknownException_StaysMasked()
    {
        var result = _filter.OnError(ErrorWith(new NullReferenceException("internal details")));

        Assert.Equal(UnexpectedError, result.Message);
        Assert.Null(result.Code);
    }

    private static IError ErrorWith(Exception exception) =>
        ErrorBuilder.New()
            .SetMessage(UnexpectedError)
            .SetException(exception)
            .Build();
}
