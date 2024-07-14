using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PSuite.Shared.Abstractions.Exceptions;

namespace PSuite.Shared.Infrastructure.Exceptions;
internal partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An exception occured");

        var response = CreateProblemDetails(exception);

        httpContext.Response.StatusCode = response.Status!.Value;

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(Exception exception)
    {
        if(exception is PSuiteException psuiteException)
        {
            return new ProblemDetails
            {
                Status = psuiteException.Category.AsStatusCode(),
                Detail = psuiteException.Message,
                Title = GetTitle(psuiteException),
                Extensions = 
                {
                    ["errorCode"] = GetErrorCode(psuiteException)
                }
            };
        }
        else
        {
            return new ProblemDetails 
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = exception.Message,
                Title = "Server error",
            };
        }

        static string GetTitle(Exception exception)
        {
            var exceptionName = exception.GetType().Name.Replace("exception", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            return string.Join(" ", SplitCamelCase().Split(exceptionName));
        }

        static string GetErrorCode(Exception exception)
        {           
            var exceptionName = exception.GetType().Name.Replace("exception", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            return string.Join("_", SplitCamelCase().Split(exceptionName)).ToLowerInvariant();
        }
    }

    [GeneratedRegex(@"(?<!^)(?=[A-Z])")]
    private static partial Regex SplitCamelCase();
}

