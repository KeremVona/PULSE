namespace HeartCDSS.Domain.Common;

/// <summary>
/// Unit of work interface for atomic transactional persistence of domain changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending tracked domain changes asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entities written to the underlying database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
