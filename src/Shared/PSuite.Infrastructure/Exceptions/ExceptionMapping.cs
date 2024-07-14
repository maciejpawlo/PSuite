using Microsoft.AspNetCore.Http;
using PSuite.Shared.Abstractions.Exceptions;

namespace PSuite.Shared.Infrastructure.Exceptions;
internal static class ExceptionMapping
{
    public static int AsStatusCode(this ExceptionCategory category) 
        => category switch 
        {
            ExceptionCategory.AlreadyExists => StatusCodes.Status409Conflict,
            ExceptionCategory.NotFound => StatusCodes.Status404NotFound,
            ExceptionCategory.ValidationError => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
}
