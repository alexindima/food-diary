using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Common.Services;

internal sealed class PostCommitActionQueue : IPostCommitActionQueue {
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromSeconds(5);
    private readonly Queue<PostCommitAction> actions = [];
    private readonly ILogger<PostCommitActionQueue> logger;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan actionTimeout;

    public PostCommitActionQueue(
        ILogger<PostCommitActionQueue> logger,
        TimeProvider timeProvider)
        : this(logger, timeProvider, DefaultActionTimeout) {
    }

    internal PostCommitActionQueue(
        ILogger<PostCommitActionQueue> logger,
        TimeProvider timeProvider,
        TimeSpan actionTimeout) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(actionTimeout, TimeSpan.Zero);
        this.logger = logger;
        this.timeProvider = timeProvider;
        this.actionTimeout = actionTimeout;
    }

    public bool HasActions => actions.Count > 0;

    public void Enqueue(string actionName, Func<CancellationToken, Task> action) {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentNullException.ThrowIfNull(action);
        actions.Enqueue(new PostCommitAction(actionName, action));
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default) {
        while (actions.TryDequeue(out PostCommitAction? action)) {
            using var timeoutSource = new CancellationTokenSource(actionTimeout, timeProvider);
            using var actionSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);
            try {
                await action.ExecuteAsync(actionSource.Token).ConfigureAwait(false);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested) {
                logger.LogWarning(
                    "Best-effort post-commit action {PostCommitActionName} exceeded timeout {PostCommitActionTimeout}.",
                    action.Name,
                    actionTimeout);
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
