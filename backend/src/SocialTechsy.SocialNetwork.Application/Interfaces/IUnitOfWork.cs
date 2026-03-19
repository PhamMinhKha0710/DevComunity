namespace SocialTechsy.SocialNetwork.Application.Interfaces;

/// <summary>
/// Unit of Work for transactional boundaries across multiple repository operations.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the given operation within a database transaction.
    /// Commits on success, rolls back on exception.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the given operation within a database transaction and returns the result.
    /// Commits on success, rolls back on exception.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
