using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot.Exceptions;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramReplyQueueTests {
    [Fact]
    public async Task SenderRemainsReservedUntilTheSuccessfulReplyCooldownEnds() {
        var clock = new ControlledBotTimeProvider();
        using var queue = new TelegramReplyQueue(clock, NullLogger<TelegramReplyQueue>.Instance);
        int sends = 0;
        Assert.False(await queue.ProcessNextAsync(CancellationToken.None));
        Assert.True(queue.Enqueue(10, _ => { sends++; return Task.CompletedTask; }));
        Assert.False(queue.Enqueue(10, _ => throw new InvalidOperationException()));

        Task<bool> processing = queue.ProcessNextAsync(CancellationToken.None);
        Assert.Equal(TimeSpan.FromSeconds(10), (await clock.NextDelayAsync()).DueTime);
        ControlledBotTimeProvider.ScheduledDelay cooldown = await clock.NextDelayAsync();
        Assert.Equal(TimeSpan.FromSeconds(1), cooldown.DueTime);
        Assert.False(queue.Enqueue(10, _ => Task.CompletedTask));
        cooldown.Fire();

        Assert.True(await processing.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));
        Assert.Equal(1, sends);
        Assert.True(queue.Enqueue(10, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task FullQueueReleasesTheRejectedSendersReservation() {
        using var queue = new TelegramReplyQueue(TimeProvider.System, NullLogger<TelegramReplyQueue>.Instance);
        for (int sender = 1; sender <= 128; sender++) {
            Assert.True(queue.Enqueue(sender, _ => throw new ApiRequestException("recipient unavailable", 403)));
        }
        Assert.False(queue.Enqueue(129, _ => Task.CompletedTask));

        Assert.True(await queue.ProcessNextAsync(CancellationToken.None));

        Assert.True(queue.Enqueue(129, _ => Task.CompletedTask));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    public async Task PermanentRejectionReleasesTheSenderWithoutRetrying(int status) {
        using var queue = new TelegramReplyQueue(TimeProvider.System, NullLogger<TelegramReplyQueue>.Instance);
        int attempts = 0;
        queue.Enqueue(10, _ => { attempts++; throw new ApiRequestException("recipient unavailable", status); });

        Assert.True(await queue.ProcessNextAsync(CancellationToken.None));

        Assert.Equal(1, attempts);
        Assert.True(queue.Enqueue(10, _ => Task.CompletedTask));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransientFailuresUseBoundedBackoffAndReleaseTheSender(bool timeOutFirstAttempt) {
        var clock = new ControlledBotTimeProvider();
        using var queue = new TelegramReplyQueue(clock, NullLogger<TelegramReplyQueue>.Instance);
        int attempts = 0;
        queue.Enqueue(10, token => {
            attempts++;
            return timeOutFirstAttempt && attempts == 1
                ? Task.Delay(Timeout.InfiniteTimeSpan, TimeProvider.System, token)
                : Task.FromException(new ApiRequestException("temporary upstream failure", 503));
        });

        Task<bool> processing = queue.ProcessNextAsync(CancellationToken.None);
        for (int attempt = 0; attempt < 3; attempt++) {
            ControlledBotTimeProvider.ScheduledDelay deadline = await clock.NextDelayAsync();
            Assert.Equal(TimeSpan.FromSeconds(10), deadline.DueTime);
            if (timeOutFirstAttempt && attempt == 0) {
                deadline.Fire();
            }
            if (attempt < 2) {
                ControlledBotTimeProvider.ScheduledDelay backoff = await clock.NextDelayAsync();
                Assert.Equal(TimeSpan.FromSeconds(2 << attempt), backoff.DueTime);
                backoff.Fire();
            }
        }

        Assert.True(await processing.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));
        Assert.Equal(3, attempts);
        Assert.True(queue.Enqueue(10, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task CallerCancellationPropagatesAndReleasesTheSender() {
        using var queue = new TelegramReplyQueue(TimeProvider.System, NullLogger<TelegramReplyQueue>.Instance);
        using var cancellation = new CancellationTokenSource();
        queue.Enqueue(10, token => Task.Delay(Timeout.InfiniteTimeSpan, token));
        Task<bool> processing = queue.ProcessNextAsync(cancellation.Token);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processing);
        Assert.True(queue.Enqueue(10, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task HostedQueueBoundsConcurrentSendsAndStopsItsWorkersCleanly() {
        using var queue = new TelegramReplyQueue(TimeProvider.System, NullLogger<TelegramReplyQueue>.Instance);
        var workersStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int sends = 0;
        for (int sender = 1; sender <= 5; sender++) {
            queue.Enqueue(sender, token => {
                if (Interlocked.Increment(ref sends) == 4) {
                    workersStarted.TrySetResult();
                }
                return Task.Delay(Timeout.InfiniteTimeSpan, token);
            });
        }
        await queue.StartAsync(CancellationToken.None);
        await workersStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(4, Volatile.Read(ref sends));

        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await queue.StopAsync(shutdown.Token);

        Assert.True(queue.ExecuteTask!.IsCompletedSuccessfully);
        Assert.True(queue.Enqueue(1, _ => Task.CompletedTask));
    }
}
