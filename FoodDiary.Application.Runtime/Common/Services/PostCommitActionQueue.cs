using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Runtime.Common.Services;

internal sealed class PostCommitActionQueue : IPostCommitActionQueue {
    private const int DefaultMaxActions = 16;
    private static readonly TimeSpan DefaultActionTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultFlushTimeout = TimeSpan.FromSeconds(10);
    private readonly Queue<PostCommitAction> _actions = [];
    private readonly ILogger<PostCommitActionQueue> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _actionTimeout;
    private readonly TimeSpan _flushTimeout;
    private readonly int _maxActions;
    private int _acceptedActionCount;

    public PostCommitActionQueue(
        ILogger<PostCommitActionQueue> logger,
        TimeProvider timeProvider)
        : this(logger, timeProvider, DefaultActionTimeout, DefaultFlushTimeout, DefaultMaxActions) {
    }

    internal PostCommitActionQueue(
        ILogger<PostCommitActionQueue> logger,
        TimeProvider timeProvider,
        TimeSpan actionTimeout)
        : this(logger, timeProvider, actionTimeout, DefaultFlushTimeout, DefaultMaxActions) {
    }

    internal PostCommitActionQueue(
        ILogger<PostCommitActionQueue> logger,
        TimeProvider timeProvider,
        TimeSpan actionTimeout,
        TimeSpan flushTimeout,
        int maxActions) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(actionTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(flushTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxActions, 1);
        _logger = logger;
        _timeProvider = timeProvider;
        _actionTimeout = actionTimeout;
        _flushTimeout = flushTimeout;
        _maxActions = maxActions;
    }

    public bool HasActions => _actions.Count > 0;

    public void Enqueue(string actionName, Func<CancellationToken, Task> action) {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentNullException.ThrowIfNull(action);

        if (_acceptedActionCount >= _maxActions) {
            ApplicationRuntimeTelemetry.RecordActionDroppedByCapacity();
            _logger.LogWarning(
                "Best-effort post-commit action {PostCommitActionName} was dropped because the queue reached capacity {PostCommitActionCapacity}.",
                actionName,
                _maxActions);
            return;
        }

        _actions.Enqueue(new PostCommitAction(actionName, action));
        _acceptedActionCount++;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default) {
        if (_actions.Count == 0) {
            return;
        }

        ApplicationRuntimeTelemetry.RecordQueueDepth(_actions.Count);
        long startedAt = _timeProvider.GetTimestamp();
        using var flushTimeoutSource = new CancellationTokenSource(_flushTimeout, _timeProvider);

        try {
            while (_actions.TryDequeue(out PostCommitAction? action)) {
                if (flushTimeoutSource.IsCancellationRequested ||
                    _timeProvider.GetElapsedTime(startedAt) >= _flushTimeout) {
                    DropUnstartedActionsAfterFlushTimeout();
                    return;
                }

                using var actionTimeoutSource = new CancellationTokenSource(_actionTimeout, _timeProvider);
                using var actionSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    flushTimeoutSource.Token,
                    actionTimeoutSource.Token);
                try {
                    await action.ExecuteAsync(actionSource.Token).ConfigureAwait(false);
                    ApplicationRuntimeTelemetry.RecordActionSucceeded();
                } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    throw;
                } catch (OperationCanceledException) when (flushTimeoutSource.IsCancellationRequested) {
                    ApplicationRuntimeTelemetry.RecordActionTimedOut();
                    _logger.LogWarning(
                        "Best-effort post-commit action {PostCommitActionName} exceeded total flush budget {PostCommitFlushTimeout}.",
                        action.Name,
                        _flushTimeout);
                    DropUnstartedActionsAfterFlushTimeout();
                    return;
                } catch (OperationCanceledException) when (actionTimeoutSource.IsCancellationRequested) {
                    ApplicationRuntimeTelemetry.RecordActionTimedOut();
                    _logger.LogWarning(
                        "Best-effort post-commit action {PostCommitActionName} exceeded timeout {PostCommitActionTimeout}.",
                        action.Name,
                        _actionTimeout);
                } catch (Exception ex) {
                    ApplicationRuntimeTelemetry.RecordActionFailed();
                    _logger.LogWarning(
                        ex,
                        "Best-effort post-commit action {PostCommitActionName} failed.",
                        action.Name);
                }
            }
        } finally {
            ApplicationRuntimeTelemetry.RecordFlushDuration(_timeProvider.GetElapsedTime(startedAt));
            if (_actions.Count == 0) {
                _acceptedActionCount = 0;
            }
        }
    }

    private void DropUnstartedActionsAfterFlushTimeout() {
        int droppedCount = _actions.Count;
        _actions.Clear();
        if (droppedCount == 0) {
            return;
        }

        ApplicationRuntimeTelemetry.RecordActionsDroppedByFlushTimeout(droppedCount);
        _logger.LogWarning(
            "Dropped {PostCommitDroppedActionCount} unstarted best-effort post-commit actions after total flush budget {PostCommitFlushTimeout} expired.",
            droppedCount,
            _flushTimeout);
    }

    private sealed record PostCommitAction(string Name, Func<CancellationToken, Task> ExecuteAsync);
}
