using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Runtime.Common.Services;

internal sealed class PostCommitActionQueue : IPostCommitActionQueue {
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromSeconds(5);
    private readonly Queue<PostCommitAction> _actions = [];
    private readonly ILogger<PostCommitActionQueue> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _actionTimeout;

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
        _logger = logger;
        _timeProvider = timeProvider;
        _actionTimeout = actionTimeout;
    }

    public bool HasActions => _actions.Count > 0;

    public void Enqueue(string actionName, Func<CancellationToken, Task> action) {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentNullException.ThrowIfNull(action);
        _actions.Enqueue(new PostCommitAction(actionName, action));
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default) {
        while (_actions.TryDequeue(out PostCommitAction? action)) {
            using var timeoutSource = new CancellationTokenSource(_actionTimeout, _timeProvider);
            using var actionSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);
            try {
                await action.ExecuteAsync(actionSource.Token).ConfigureAwait(false);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested) {
                _logger.LogWarning(
                    "Best-effort post-commit action {PostCommitActionName} exceeded timeout {PostCommitActionTimeout}.",
                    action.Name,
                    _actionTimeout);
            } catch (Exception ex) {
                _logger.LogWarning(
                    ex,
                    "Best-effort post-commit action {PostCommitActionName} failed.",
                    action.Name);
            }
        }
    }

    private sealed record PostCommitAction(string Name, Func<CancellationToken, Task> ExecuteAsync);
}
