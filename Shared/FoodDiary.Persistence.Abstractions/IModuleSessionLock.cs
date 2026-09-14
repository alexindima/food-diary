namespace FoodDiary.Persistence.Abstractions;

/// <summary>Acquires an independent database session lease without saving or retrying caller work.</summary>
public interface IModuleSessionLock {
    /// <summary>The caller owns disposal; the lease outlives transactions on the scoped context.</summary>
    Task<IAsyncDisposable> AcquireAsync(long lockKey, CancellationToken cancellationToken = default);
}
