namespace FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

/// <summary>
/// Queues non-transactional side effects that must run only after a command's unit of work commits successfully.
/// </summary>
public interface IPostCommitActionQueue {
    bool HasActions { get; }

    void Enqueue(Func<CancellationToken, Task> action);

    Task FlushAsync(CancellationToken cancellationToken = default);
}
