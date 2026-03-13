namespace SocialTechsy.SocialNetwork.Application.Common;

/// <summary>
/// Marker interface for commands that require transactional execution.
/// Handlers for these requests will run within a database transaction.
/// </summary>
public interface ITransactionalRequest
{
}
