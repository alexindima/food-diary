using System.Net;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramOperationWorkerLoopTests {
    [Fact]
    public async Task DisabledWorker_DoesNotPollTheApi() {
        using var handler = new Handler((_, _) => throw new InvalidOperationException("Disabled worker must remain idle"));
        using var telegramHttp = new HttpClient(handler);
        using TelegramOperationWorker worker = CreateWorker(handler, telegramHttp, TimeProvider.System, enabled: false);
        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact]
    public async Task DiscoveryFailure_RetriesAndHandlesCompetingLease() {
        var clock = new ControlledBotTimeProvider();
        int discoveries = 0;
        int leases = 0;
        var id = Guid.NewGuid();
        using var handler = new Handler((request, _) => {
            if (request.RequestUri!.AbsolutePath.EndsWith("/ready", StringComparison.Ordinal)) {
                if (Interlocked.Increment(ref discoveries) == 1) {
                    throw new HttpRequestException("Discovery unavailable");
                }
                return Task.FromResult(BotOperationScenario.Json(new[] { id }));
            }
            Interlocked.Increment(ref leases);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict));
        });
        using var telegramHttp = new HttpClient(handler);
        using TelegramOperationWorker worker = CreateWorker(handler, telegramHttp, clock);
        await worker.StartAsync(CancellationToken.None);
        ControlledBotTimeProvider.ScheduledDelay first = await clock.NextDelayAsync();
        Assert.Equal(TimeSpan.FromSeconds(5), first.DueTime);
        first.Fire();
        await clock.NextDelayAsync();
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        Assert.Equal(2, discoveries);
        Assert.Equal(1, leases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_InterruptsDiscoveryOrProcessingWithoutRetry(bool duringProcessing) {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var id = Guid.NewGuid();
        using var handler = new Handler(async (request, cancellationToken) => {
            if (duringProcessing && request.RequestUri!.AbsolutePath.EndsWith("/ready", StringComparison.Ordinal)) {
                return BotOperationScenario.Json(new[] { id });
            }
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation must interrupt API request");
        });
        using var telegramHttp = new HttpClient(handler);
        using TelegramOperationWorker worker = CreateWorker(handler, telegramHttp, TimeProvider.System);
        await worker.StartAsync(CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact]
    public async Task FailedOperation_DoesNotPreventTheNextOperationInTheBatch() {
        var clock = new ControlledBotTimeProvider();
        int leases = 0;
        using var handler = new Handler((request, _) => {
            if (request.RequestUri!.AbsolutePath.EndsWith("/ready", StringComparison.Ordinal)) {
                return Task.FromResult(BotOperationScenario.Json(new[] { Guid.NewGuid(), Guid.NewGuid() }));
            }
            int attempt = Interlocked.Increment(ref leases);
            return Task.FromResult(new HttpResponseMessage(attempt == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.Conflict));
        });
        using var telegramHttp = new HttpClient(handler);
        using TelegramOperationWorker worker = CreateWorker(handler, telegramHttp, clock);
        await worker.StartAsync(CancellationToken.None);
        await clock.NextDelayAsync();
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        Assert.Equal(2, leases);
    }

    private static TelegramOperationWorker CreateWorker(HttpMessageHandler handler, HttpClient telegramHttp, TimeProvider clock, bool enabled = true) =>
        new(new Factory(handler), Options.Create(new TelegramBotOptions { OperationsEnabled = enabled, ApiBaseUrl = "https://diary.example", ApiSecret = "test-secret" }),
            new TelegramBotClient("123:test", telegramHttp), clock, NullLogger<TelegramOperationWorker>.Instance);

    [ExcludeFromCodeCoverage]
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken);
    }
}
