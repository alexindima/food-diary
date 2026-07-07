using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Common.Services;

internal sealed class PostCommitActionQueue(ILogger<PostCommitActionQueue> logger) : IPostCommitActionQueue {
    private readonly Queue<PostCommitAction> actions = [];

    public bool HasActions => actions.Count > 0;

    public void Enqueue(string actionName, Func<CancellationToken, Task> action) {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentNullException.ThrowIfNull(action);
        actions.Enqueue(new PostCommitAction(actionName, action));
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default) {
        while (actions.TryDequeue(out PostCommitAction? action)) {
            try {
                await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception ex) {
                logger.LogWarning(
                    ex,
                    "Best-effort post-commit action {PostCommitActionName} failed.",
                    action.Name);
            }
        }
    }

    private sealed record PostCommitAction(string Name, Func<CancellationToken, Task> ExecuteAsync);
}
