using Microsoft.AspNetCore.Http;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Api.Core;

// Maps a failed Result to ProblemDetails. Error.Type decides the status code.
public static class ApiResults
{
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot create a problem from a successful result.");

        var error = result.Error;
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad request"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "Server failure")
        };

        return Results.Problem(
            statusCode: status,
            title: title,
            detail: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
