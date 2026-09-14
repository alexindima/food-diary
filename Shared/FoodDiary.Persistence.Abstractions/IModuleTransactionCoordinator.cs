using System.Data.Common;

namespace FoodDiary.Persistence.Abstractions;

/// <summary>Coordinates a top-level transaction across the scoped module contexts and shared unit of work.</summary>
public interface IModuleTransactionCoordinator {
    /// <summary>The live caller transaction, including transactions begun after a repository was resolved.</summary>
    DbTransaction? CurrentTransaction { get; }

    /// <summary>
    /// Requires a clean scope. Retries reset tracked changes and post-commit actions; a failed Result rolls back.
    /// The operation may use the transaction for owner SQL but must not commit or dispose it.
    /// Saving and transaction completion remain owned by the shared coordinator.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<DbTransaction, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
