using HotChocolate.Execution;

namespace MyApi.GraphQL;

public class DomainErrorFilter : IErrorFilter
{
    public IError OnError(IError error) => error.Exception switch
    {
        UnauthorizedAccessException => Expose(error, "FORBIDDEN"),
        InvalidOperationException or ArgumentException => Expose(error, "INVALID_INPUT"),
        _ => error
    };

    private static IError Expose(IError error, string code) =>
        ErrorBuilder.FromError(error)
            .SetMessage(error.Exception!.Message)
            .SetException(null)
            .SetCode(code)
            .Build();
}
