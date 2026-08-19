using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MIC.risk.Extensions;

/// <summary>
/// Writes RFC 9457 problem responses from places that sit outside MVC — the authentication
/// events and the exception middleware — so their bodies are indistinguishable from the ones
/// <see cref="ControllerBase.Problem(string, string, int?, string, string)"/> produces.
/// </summary>
public static class ProblemResponseWriter
{
    private const string ProblemContentType = "application/problem+json";

    public static string TypeForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        _ => "about:blank"
    };

    public static Task WriteAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        var problem = new ProblemDetails
        {
            Type = TypeForStatus(statusCode),
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = statusCode;

        // The content type has to be passed here: the two-argument WriteAsJsonAsync overload
        // resets it to application/json and would quietly undo an earlier assignment.
        return context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: ProblemContentType,
            cancellationToken: context.RequestAborted);
    }
}
