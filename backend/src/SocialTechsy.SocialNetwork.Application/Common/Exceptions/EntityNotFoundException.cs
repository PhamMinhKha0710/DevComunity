namespace SocialTechsy.SocialNetwork.Application.Common.Exceptions;

/// <summary>
/// Thrown when an entity is not found (maps to HTTP 404).
/// </summary>
public class EntityNotFoundException : Exception
{
    public string EntityType { get; }
    public object? EntityId { get; }

    public EntityNotFoundException(string entityType, object? entityId = null)
        : base(FormatMessage(entityType, entityId))
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    private static string FormatMessage(string entityType, object? entityId)
        => entityId != null
            ? $"{entityType} with ID '{entityId}' was not found."
            : $"{entityType} was not found.";
}
