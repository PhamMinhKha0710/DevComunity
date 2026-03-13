using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using SocialTechsy.SocialNetwork.Application.Common.Exceptions;

namespace SocialTechsy.SocialNetwork.Api.ExceptionHandling;

/// <summary>
/// Handles UnauthorizedCommandException and returns 403 Forbidden.
/// </summary>
public class UnauthorizedCommandExceptionHandler : IExceptionHandler
{
    private readonly ILogger<UnauthorizedCommandExceptionHandler> _logger;

    public UnauthorizedCommandExceptionHandler(ILogger<UnauthorizedCommandExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UnauthorizedCommandException ex)
            return false;

        _logger.LogWarning("Unauthorized command: {Message}", ex.Message);

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            title = "Forbidden",
            status = StatusCodes.Status403Forbidden,
            detail = ex.Message
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            cancellationToken);

        return true;
    }
}
