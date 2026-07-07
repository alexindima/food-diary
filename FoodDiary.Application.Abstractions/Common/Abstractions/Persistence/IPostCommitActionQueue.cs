namespace FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

/// <summary>
/// Queues best-effort real-time side effects that must run only after a command's unit of work commits successfully.
/// </summary>
/// <remarks>
/// This queue is in-memory and is not a durable delivery mechanism. Critical side effects must be persisted through
/// a transactional outbox instead.
/// </remarks>
public interface IPostCommitActionQueue {
    bool HasActions { get; }

    void Enqueue(string actionName, Func<CancellationToken, Task> action);

    Task FlushAsync(CancellationToken cancellationToken = default);
}
