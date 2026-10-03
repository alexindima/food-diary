namespace FoodDiary.Application.Contracts.Common.Abstractions.Persistence;

public interface IReadSnapshotExecutor {
    Task<T> ExecuteAsync<T>(ReadSnapshotBudget budget, Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
