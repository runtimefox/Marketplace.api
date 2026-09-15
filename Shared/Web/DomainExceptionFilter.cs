using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyApi.Shared.Errors;

namespace MyApi.Shared.Web;

public class DomainExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (status, detail) = context.Exception switch
        {
            UnauthorizedAccessException exception => (StatusCodes.Status403Forbidden, exception.Message),
            ArgumentException exception => (StatusCodes.Status400BadRequest, ExceptionMessages.WithoutParameterName(exception)),
            InvalidOperationException exception => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (0, string.Empty)
        };

        if (status == 0)
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails { Status = status, Detail = detail })
        {
            StatusCode = status
        };
        context.ExceptionHandled = true;
    }
}
