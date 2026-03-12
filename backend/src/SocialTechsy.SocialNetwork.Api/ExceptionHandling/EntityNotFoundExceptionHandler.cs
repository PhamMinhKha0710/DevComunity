using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using SocialTechsy.SocialNetwork.Application.Common.Exceptions;

namespace SocialTechsy.SocialNetwork.Api.ExceptionHandling;

/// <summary>
/// Handles EntityNotFoundException and returns 404 Not Found.
/// </summary>
public class EntityNotFoundExceptionHandler : IExceptionHandler
{
    private readonly ILogger<EntityNotFoundExceptionHandler> _logger;

    public EntityNotFoundExceptionHandler(ILogger<EntityNotFoundExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not EntityNotFoundException ex)
            return false;

        _logger.LogWarning("Entity not found: {EntityType} {EntityId}", ex.EntityType, ex.EntityId);

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            title = "Not Found",
            status = StatusCodes.Status404NotFound,
            detail = ex.Message
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            cancellationToken);

        return true;
    }
}
