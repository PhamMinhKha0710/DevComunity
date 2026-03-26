using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace SocialTechsy.SocialNetwork.Api.ExceptionHandling;

/// <summary>
/// Catches all unhandled exceptions and returns a safe 500 response.
/// Prevents internal error details from leaking in production.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception,
            "Unhandled exception: {Type} - {Message}\n{StackTrace}",
            exception.GetType().Name,
            exception.Message,
            exception.StackTrace);

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "An unexpected error occurred. Please try again later.",
            error = exception.Message
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }),
            cancellationToken);

        return true;
    }
}
