namespace FoodDiary.Persistence.Abstractions;

/// <summary>Serializes a scoped operation without opening a database transaction around its callback.</summary>
public interface IModuleSessionCoordinator {
    /// <summary>
    /// Requires a clean scope and holds a separate session advisory lease until completion.
    /// Executes the callback once; only persistence may retry. Successful results always save through
    /// the shared unit of work. Failures discard unsaved tracking but preserve intermediate saves.
    /// </summary>
    Task<T> ExecuteSerializedAsync<T>(
        string serializationKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
