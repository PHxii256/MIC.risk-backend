using MIC.risk.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MIC.risk.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away — a cancelled query is not an error worth reporting.
            _logger.LogDebug("Request {Path} was aborted by the client.", context.Request.Path);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            InvalidOperationException =>
                (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),

            DbUpdateException =>
                (StatusCodes.Status400BadRequest, "Bad Request",
                    "A database update error occurred. Check your input and try again."),

            SqlException sqlEx when sqlEx.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase) =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error",
                    "Database schema is out of date. Run 'dotnet ef database update' and restart the app."),

            UnauthorizedAccessException =>
                (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),

            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                    "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        if (context.Response.HasStarted)
        {
            // Too late to replace the body; let the original response stand.
            _logger.LogWarning("Response had already started; the error could not be written as ProblemDetails.");
            return;
        }

        context.Response.Clear();

        await ProblemResponseWriter.WriteAsync(context, statusCode, title, detail);
    }
}
