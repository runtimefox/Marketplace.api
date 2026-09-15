namespace MyApi.Shared.Errors;

public static class ExceptionMessages
{
    public static string WithoutParameterName(ArgumentException exception) =>
        exception.ParamName is null
            ? exception.Message
            : exception.Message.Replace($" (Parameter '{exception.ParamName}')", string.Empty);
}
