using HotChocolate.Execution;
using MyApi.Shared.Errors;

namespace MyApi.GraphQL;

public class DomainErrorFilter : IErrorFilter
{
    public IError OnError(IError error) => error.Exception switch
    {
        UnauthorizedAccessException exception => Expose(error, exception.Message, "FORBIDDEN"),
        ArgumentException exception => Expose(error, ExceptionMessages.WithoutParameterName(exception), "INVALID_INPUT"),
        InvalidOperationException exception => Expose(error, exception.Message, "INVALID_INPUT"),
        _ => error
    };

    private static IError Expose(IError error, string message, string code) =>
        ErrorBuilder.FromError(error)
            .SetMessage(message)
            .SetException(null)
            .SetCode(code)
            .Build();
}
