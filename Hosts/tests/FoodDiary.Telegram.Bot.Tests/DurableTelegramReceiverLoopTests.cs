using System.Net;
using System.Net.Http.Json;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class DurableTelegramReceiverLoopTests {
    [Fact]
    public async Task Intake_BackoffIsBoundedAndResetsAfterSuccessfulPersistence() {
        var clock = new ControlledBotTimeProvider();
        int requests = 0;
        using var handler = new Handler((_, _) => {
            int attempt = Interlocked.Increment(ref requests);
            if (attempt == 7) {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = JsonContent.Create(new { ok = true, result = new[] { new { update_id = 10 } } }),
                });
            }
            throw new HttpRequestException("Transient network failure");
        });
        using var http = new HttpClient(handler);
        var persisted = new List<int>();
        var receiver = new DurableTelegramReceiver(new TelegramBotClient("123:test", http), (update, _) => {
            persisted.Add(update.Id);
            return Task.CompletedTask;
        }, clock, NullLogger.Instance);
        using var stopping = new CancellationTokenSource();
        Task run = receiver.RunAsync(stopping.Token);
        foreach (int seconds in new[] { 2, 4, 8, 16, 30, 30 }) {
            ControlledBotTimeProvider.ScheduledDelay delay = await clock.NextDelayAsync();
            Assert.Equal(TimeSpan.FromSeconds(seconds), delay.DueTime);
            Assert.Null(receiver.NextOffset);
            delay.Fire();
        }
        ControlledBotTimeProvider.ScheduledDelay resetDelay = await clock.NextDelayAsync();
        Assert.Equal(TimeSpan.FromSeconds(2), resetDelay.DueTime);
        Assert.Equal(new[] { 10 }, persisted);
        Assert.Equal(11, receiver.NextOffset);
        await stopping.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));
    }

    [Fact]
    public async Task Intake_CancellationDuringLongPollStopsWithoutAcknowledgingAnyUpdate() {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new Handler(async (_, cancellationToken) => {
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation must interrupt long polling");
        });
        using var http = new HttpClient(handler);
        var receiver = new DurableTelegramReceiver(new TelegramBotClient("123:test", http), (_, _) => throw new InvalidOperationException("Nothing to persist"),
            TimeProvider.System, NullLogger.Instance);
        using var stopping = new CancellationTokenSource();
        Task run = receiver.RunAsync(stopping.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await stopping.CancelAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        Assert.Null(receiver.NextOffset);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken);
    }
}
