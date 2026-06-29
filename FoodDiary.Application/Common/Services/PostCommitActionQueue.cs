using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

namespace FoodDiary.Application.Common.Services;

internal sealed class PostCommitActionQueue : IPostCommitActionQueue {
    private readonly List<Func<CancellationToken, Task>> actions = [];

    public bool HasActions => actions.Count > 0;

    public void Enqueue(Func<CancellationToken, Task> action) {
        ArgumentNullException.ThrowIfNull(action);
        actions.Add(action);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default) {
        if (actions.Count == 0) {
            return;
        }

        Func<CancellationToken, Task>[] pendingActions = [.. actions];
        actions.Clear();

        foreach (Func<CancellationToken, Task> action in pendingActions) {
            await action(cancellationToken).ConfigureAwait(false);
        }
    }
}
