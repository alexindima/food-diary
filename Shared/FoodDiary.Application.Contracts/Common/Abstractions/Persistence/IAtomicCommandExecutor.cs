namespace FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

/// <summary>Executes and saves a top-level command atomically without exposing database capabilities.</summary>
public interface IAtomicCommandExecutor {
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
