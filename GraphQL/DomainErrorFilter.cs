using HotChocolate.Execution;

namespace MyApi.GraphQL;

public class DomainErrorFilter : IErrorFilter
{
    public IError OnError(IError error) => error.Exception switch
    {
        UnauthorizedAccessException exception => Expose(error, exception.Message, "FORBIDDEN"),
        ArgumentException exception => Expose(error, WithoutParameterName(exception), "INVALID_INPUT"),
        InvalidOperationException exception => Expose(error, exception.Message, "INVALID_INPUT"),
        _ => error
    };

    private static string WithoutParameterName(ArgumentException exception) =>
        exception.ParamName is null
            ? exception.Message
            : exception.Message.Replace($" (Parameter '{exception.ParamName}')", string.Empty);

    private static IError Expose(IError error, string message, string code) =>
        ErrorBuilder.FromError(error)
            .SetMessage(message)
            .SetException(null)
            .SetCode(code)
            .Build();
}
