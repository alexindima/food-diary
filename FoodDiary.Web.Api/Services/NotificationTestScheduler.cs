using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Results;
using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Services;

public sealed class NotificationTestScheduler(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    IOptions<NotificationTestSchedulerOptions> options,
    ILogger<NotificationTestScheduler> logger)
    : BackgroundService, INotificationTestScheduler {
    private readonly Lock _sync = new();
    private readonly PriorityQueue<ScheduledItem, DateTime> _pending = new();
    private readonly SemaphoreSlim _changed = new(0, 1);
    private readonly int _maxPending = options.Value.MaxPending;

    public Task<Result<ScheduledNotificationData>> ScheduleAsync(
        Guid userId,
        int delaySeconds,
        string type,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        int normalizedDelaySeconds = Math.Clamp(delaySeconds, 1, 3600);
        string normalizedType = NormalizeType(type);
        DateTime scheduledAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(normalizedDelaySeconds);

        lock (_sync) {
            if (_pending.Count >= _maxPending) {
                return Task.FromResult(Result.Failure<ScheduledNotificationData>(
                    NotificationErrors.TestScheduleCapacityExceeded()));
            }

            _pending.Enqueue(new ScheduledItem(userId, normalizedType, scheduledAtUtc), scheduledAtUtc);
        }

        SignalChanged();
        return Task.FromResult(Result.Success(new ScheduledNotificationData(
            normalizedType,
            normalizedDelaySeconds,
            scheduledAtUtc)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            ScheduledItem? dueItem = TryTakeDueItem();
            if (dueItem is not null) {
                await DispatchAsync(dueItem, stoppingToken).ConfigureAwait(false);
                continue;
            }

            TimeSpan? delay = GetDelayUntilNextItem();
            try {
                if (delay is null) {
                    await _changed.WaitAsync(stoppingToken).ConfigureAwait(false);
                } else {
                    await WaitForDelayOrChangeAsync(delay.Value, stoppingToken).ConfigureAwait(false);
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
        }
    }

    private ScheduledItem? TryTakeDueItem() {
        lock (_sync) {
            if (!_pending.TryPeek(out ScheduledItem? item, out DateTime scheduledAtUtc) ||
                scheduledAtUtc > timeProvider.GetUtcNow().UtcDateTime) {
                return null;
            }

            _pending.Dequeue();
            return item;
        }
    }

    private TimeSpan? GetDelayUntilNextItem() {
        lock (_sync) {
            if (!_pending.TryPeek(out _, out DateTime scheduledAtUtc)) {
                return null;
            }

            TimeSpan delay = scheduledAtUtc - timeProvider.GetUtcNow().UtcDateTime;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }
    }

    private async Task WaitForDelayOrChangeAsync(TimeSpan delay, CancellationToken cancellationToken) {
        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var delayTask = Task.Delay(delay, timeProvider, waitCancellation.Token);
        Task changeTask = _changed.WaitAsync(waitCancellation.Token);
        await Task.WhenAny(delayTask, changeTask).ConfigureAwait(false);
        await waitCancellation.CancelAsync().ConfigureAwait(false);
        try {
            await Task.WhenAll(delayTask, changeTask).ConfigureAwait(false);
        } catch (OperationCanceledException) when (waitCancellation.IsCancellationRequested) {
        }
    }

    private void SignalChanged() {
        try {
            _changed.Release();
        } catch (SemaphoreFullException) {
        }
    }

    private async Task DispatchAsync(ScheduledItem item, CancellationToken cancellationToken) {
        try {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            ITestNotificationDeliveryDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<ITestNotificationDeliveryDispatcher>();
            await dispatcher.DispatchAsync(item.UserId, item.Type, cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        } catch (Exception ex) {
            logger.LogError(ex, "Failed to deliver scheduled test notification for user {UserId}.", item.UserId);
        }
    }

    private static string NormalizeType(string? type) {
        if (string.IsNullOrWhiteSpace(type)) {
            return NotificationTypes.FastingCompleted;
        }

        return type.Trim() switch {
            NotificationTypes.FastingCompleted => NotificationTypes.FastingCompleted,
            NotificationTypes.FastingCheckInReminder => NotificationTypes.FastingCheckInReminder,
            NotificationTypes.EatingWindowStarted => NotificationTypes.EatingWindowStarted,
            NotificationTypes.FastingWindowStarted => NotificationTypes.FastingWindowStarted,
            _ => NotificationTypes.FastingCompleted,
        };
    }

    private sealed record ScheduledItem(Guid UserId, string Type, DateTime ScheduledAtUtc);
}
