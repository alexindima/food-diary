namespace FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

public interface IPostCommitActionQueue {
    bool HasActions { get; }

    void Enqueue(Func<CancellationToken, Task> action);

    Task FlushAsync(CancellationToken cancellationToken = default);
}
