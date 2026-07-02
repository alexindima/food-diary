using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

namespace FoodDiary.Application.Common.Services;

internal sealed class PostCommitActionQueue : IPostCommitActionQueue {
    private readonly Queue<Func<CancellationToken, Task>> actions = [];

    public bool HasActions => actions.Count > 0;

    public void Enqueue(Func<CancellationToken, Task> action) {
        ArgumentNullException.ThrowIfNull(action);
        actions.Enqueue(action);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default) {
        while (actions.TryDequeue(out Func<CancellationToken, Task>? action)) {
            await action(cancellationToken).ConfigureAwait(false);
        }
    }
}
