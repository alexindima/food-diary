using System.Net;
using System.Text;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
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
        var service = new StubBillingWebhookInboxService(new BillingWebhookInboxRunResult(processed, failed));
        var tracker = new JobExecutionStateTracker();
        var job = new BillingWebhookInboxJob(
            service,
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingWebhookInboxJob>.Instance);

        await job.Execute();

        Assert.Equal(0, tracker.GetSnapshot("billing.webhook-inbox")?.ConsecutiveFailures);
        Assert.Equal(100, service.LastBatchSize);
    }

    [Fact]
    public async Task BillingWebhookInboxJob_WhenServiceFails_RecordsFailureAndRethrows() {
        var service = new StubBillingWebhookInboxService(new InvalidOperationException("inbox failed"));
        var tracker = new JobExecutionStateTracker();
        var job = new BillingWebhookInboxJob(
            service,
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingWebhookInboxJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());

        Assert.Equal(1, tracker.GetSnapshot("billing.webhook-inbox")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task BillingWebhookInboxJob_WhenCanceled_RecordsCancellationAndRethrows() {
        var service = new StubBillingWebhookInboxService(cancel: true);
        var tracker = new JobExecutionStateTracker();
        var job = new BillingWebhookInboxJob(
            service,
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
        var job = new PaddleNotificationRecoveryJob(
            CreateRecoveryService(handler),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<PaddleNotificationRecoveryJob>.Instance);

        await job.Execute();

        Assert.Equal(0, tracker.GetSnapshot("billing.paddle-notification-recovery")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task PaddleNotificationRecoveryJob_WhenServiceFails_RecordsFailureAndRethrows() {
        var tracker = new JobExecutionStateTracker();
        var job = new PaddleNotificationRecoveryJob(
            CreateRecoveryService(new ThrowingHandler(new HttpRequestException("Paddle failed"))),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<PaddleNotificationRecoveryJob>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => job.Execute());

        Assert.Equal(1, tracker.GetSnapshot("billing.paddle-notification-recovery")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task PaddleNotificationRecoveryJob_WhenCanceled_RecordsCancellationAndRethrows() {
        var tracker = new JobExecutionStateTracker();
        var job = new PaddleNotificationRecoveryJob(
            CreateRecoveryService(new ThrowingHandler(new OperationCanceledException())),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<PaddleNotificationRecoveryJob>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.Execute(cancellationTokenSource.Token));

        Assert.Equal(0, tracker.GetSnapshot("billing.paddle-notification-recovery")?.ConsecutiveFailures);
    }

    private static PaddleNotificationRecoveryService CreateRecoveryService(HttpMessageHandler handler) =>
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
            }));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [ExcludeFromCodeCoverage]
    private sealed class StubBillingWebhookInboxService : IBillingWebhookInboxService {
        private readonly BillingWebhookInboxRunResult? _result;
        private readonly Exception? _exception;
        private readonly bool _cancel;

        public StubBillingWebhookInboxService(BillingWebhookInboxRunResult result) => _result = result;
        public StubBillingWebhookInboxService(Exception exception) => _exception = exception;
        public StubBillingWebhookInboxService(bool cancel) => _cancel = cancel;
        public int LastBatchSize { get; private set; }

        public Task<FoodDiary.Results.Result> ProcessAsync(Guid webhookEventId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BillingWebhookInboxRunResult> ProcessPendingAsync(
            int batchSize,
            CancellationToken cancellationToken = default) {
            LastBatchSize = batchSize;
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
