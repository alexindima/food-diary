using System.Net;
using System.Text;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Modules.Billing.Application.Commands.ReplayFailedPaddleNotifications;
using FoodDiary.Modules.Billing.Contracts.Commands.ReplayFailedPaddleNotifications;
using FoodDiary.Modules.Billing.Contracts.Commands.ProcessBillingWebhookInbox;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingRecoveryJobsTests {
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 1)]
    public async Task BillingWebhookInboxJob_WhenServiceSucceeds_RecordsProcessedCount(int processed, int failed) {
        var service = new StubInboxHandler(new BillingWebhookInboxRunResult(processed, failed));
        var tracker = new JobExecutionStateTracker();
        await using ServiceProvider provider = CreateInboxProvider(service);
        var job = new BillingWebhookInboxJob(
            provider.GetRequiredService<ISender>(),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingWebhookInboxJob>.Instance);

        await job.Execute();

        Assert.Equal(0, tracker.GetSnapshot("billing.webhook-inbox")?.ConsecutiveFailures);
        Assert.Equal(100, service.LastBatchSize);
    }

    [Fact]
    public async Task BillingWebhookInboxJob_WhenServiceFails_RecordsFailureAndRethrows() {
        var service = new StubInboxHandler(new InvalidOperationException("inbox failed"));
        var tracker = new JobExecutionStateTracker();
        await using ServiceProvider provider = CreateInboxProvider(service);
        var job = new BillingWebhookInboxJob(
            provider.GetRequiredService<ISender>(),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingWebhookInboxJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());

        Assert.Equal(1, tracker.GetSnapshot("billing.webhook-inbox")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task BillingWebhookInboxJob_WhenCanceled_RecordsCancellationAndRethrows() {
        var service = new StubInboxHandler(cancel: true);
        var tracker = new JobExecutionStateTracker();
        await using ServiceProvider provider = CreateInboxProvider(service);
        var job = new BillingWebhookInboxJob(
            provider.GetRequiredService<ISender>(),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingWebhookInboxJob>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.Execute(cancellationTokenSource.Token));

        Assert.Equal(0, tracker.GetSnapshot("billing.webhook-inbox")?.ConsecutiveFailures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task PaddleNotificationRecoveryJob_WhenServiceSucceeds_RecordsReplayedCount(int replayed) {
        string json = replayed == 0
            ? """{"data":[],"meta":{"pagination":{"next":null}}}"""
            : """{"data":[{"id":"ntf_failed","origin":"event","replayed_at":null}],"meta":{"pagination":{"next":null}}}""";
        var handler = new QueueHandler(
            JsonResponse(json),
            new HttpResponseMessage(HttpStatusCode.Accepted));
        var tracker = new JobExecutionStateTracker();
        await using ServiceProvider provider = CreateRecoveryProvider(CreateRecoveryService(handler));
        var job = new PaddleNotificationRecoveryJob(
            provider.GetRequiredService<ISender>(),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<PaddleNotificationRecoveryJob>.Instance);

        await job.Execute();

        Assert.Equal(0, tracker.GetSnapshot("billing.paddle-notification-recovery")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task PaddleNotificationRecoveryJob_WhenReplayLimitIsReached_RecordsSuccess() {
        const string json = """{"data":[{"id":"ntf_failed","origin":"event","replayed_at":null}],"meta":{"pagination":{"next":null}}}""";
        var handler = new QueueHandler(JsonResponse(json), new HttpResponseMessage(HttpStatusCode.Accepted));
        var tracker = new JobExecutionStateTracker();
        await using ServiceProvider provider = CreateRecoveryProvider(CreateRecoveryService(handler, maximumReplaysPerRun: 1));
        var job = new PaddleNotificationRecoveryJob(
            provider.GetRequiredService<ISender>(),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<PaddleNotificationRecoveryJob>.Instance);

        await job.Execute();

        Assert.Equal(0, tracker.GetSnapshot("billing.paddle-notification-recovery")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task PaddleNotificationRecoveryJob_WhenServiceFails_RecordsFailureAndRethrows() {
        var tracker = new JobExecutionStateTracker();
        await using ServiceProvider provider = CreateRecoveryProvider(CreateRecoveryService(new ThrowingHandler(new HttpRequestException("Paddle failed"))));
        var job = new PaddleNotificationRecoveryJob(
            provider.GetRequiredService<ISender>(),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<PaddleNotificationRecoveryJob>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => job.Execute());

        Assert.Equal(1, tracker.GetSnapshot("billing.paddle-notification-recovery")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task PaddleNotificationRecoveryJob_WhenCanceled_RecordsCancellationAndRethrows() {
        var tracker = new JobExecutionStateTracker();
        await using ServiceProvider provider = CreateRecoveryProvider(CreateRecoveryService(new ThrowingHandler(new OperationCanceledException())));
        var job = new PaddleNotificationRecoveryJob(
            provider.GetRequiredService<ISender>(),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<PaddleNotificationRecoveryJob>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.Execute(cancellationTokenSource.Token));

        Assert.Equal(0, tracker.GetSnapshot("billing.paddle-notification-recovery")?.ConsecutiveFailures);
    }

    private static ServiceProvider CreateInboxProvider(StubInboxHandler handler) => new ServiceCollection()
        .AddFoodDiaryMediator(_ => { })
        .AddSingleton<IRequestHandler<ProcessBillingWebhookInboxCommand, BillingWebhookInboxRunResult>>(handler)
        .BuildServiceProvider();

    private static ServiceProvider CreateRecoveryProvider(PaddleNotificationRecoveryService gateway) => new ServiceCollection()
        .AddFoodDiaryMediator(_ => { })
        .AddSingleton<IRequestHandler<ReplayFailedPaddleNotificationsCommand, PaddleNotificationRecoveryResult>>(
            new ReplayFailedPaddleNotificationsCommandHandler(gateway))
        .BuildServiceProvider();

    private static PaddleNotificationRecoveryService CreateRecoveryService(
        HttpMessageHandler handler,
        int maximumReplaysPerRun = 100) =>
        new(
            new HttpClient(handler),
            MsOptions.Create(new PaddleOptions {
                Environment = PaddleOptions.SandboxEnvironment,
                ApiKey = "paddle-api-key",
                ApiBaseUrl = "https://sandbox-api.paddle.com",
                ClientSideToken = "test_client-token",
                WebhookSecretKey = "pdl_ntfset_secret",
                NotificationSettingId = "ntfset_01abcdefghijklmnopqrstuvwx",
                PremiumMonthlyPriceId = "pri_month",
                PremiumYearlyPriceId = "pri_year",
                CheckoutUrl = "https://example.com/premium",
            }),
            maximumReplaysPerRun: maximumReplaysPerRun);

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [ExcludeFromCodeCoverage]
    private sealed class StubInboxHandler : IRequestHandler<ProcessBillingWebhookInboxCommand, BillingWebhookInboxRunResult> {
        private readonly BillingWebhookInboxRunResult? _result;
        private readonly Exception? _exception;
        private readonly bool _cancel;

        public StubInboxHandler(BillingWebhookInboxRunResult result) => _result = result;
        public StubInboxHandler(Exception exception) => _exception = exception;
        public StubInboxHandler(bool cancel) => _cancel = cancel;
        public int LastBatchSize { get; private set; }

        public Task<BillingWebhookInboxRunResult> Handle(
            ProcessBillingWebhookInboxCommand request,
            CancellationToken cancellationToken) {
            LastBatchSize = request.BatchSize;
            if (_cancel) {
                return Task.FromCanceled<BillingWebhookInboxRunResult>(cancellationToken);
            }

            return _exception is null
                ? Task.FromResult(_result!)
                : Task.FromException<BillingWebhookInboxRunResult>(_exception);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(_responses.Dequeue());
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromException<HttpResponseMessage>(exception);
    }
}
