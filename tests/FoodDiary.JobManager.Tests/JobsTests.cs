using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;

namespace FoodDiary.JobManager.Tests;

public sealed class JobsTests {
    private const string JobManagerMeterName = "FoodDiary.JobManager";

    [Fact]
    public async Task ImageCleanupJob_RecordsSuccessMetrics() {
        long? executionCount = null;
        string? outcome = null;
        long? deletedItems = null;
        double? duration = null;

        using var listener = CreateJobManagerListener(
            expectedJobName: "images.cleanup",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: (value, _) => deletedItems = value,
            onDuration: (value, _) => duration = value);

        var cleanupService = new RecordingImageCleanupService([2, 0]);
        var options = Options.Create(new ImageCleanupOptions { BatchSize = 2, OlderThanHours = 12 });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new ImageCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            tracker,
            NullLogger<ImageCleanupJob>.Instance);

        await job.Execute();

        Assert.Equal(1, executionCount);
        Assert.Equal("success", outcome);
        Assert.Equal(2, deletedItems);
        Assert.NotNull(duration);
        Assert.True(duration >= 0);
        Assert.Equal(0, tracker.GetSnapshot("images.cleanup")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("images.cleanup")?.LastSucceededAtUtc);
    }

    [Fact]
    public async Task UserCleanupJob_RecordsFailureMetric_AndRethrows() {
        long? executionCount = null;
        string? outcome = null;
        double? duration = null;

        using var listener = CreateJobManagerListener(
            expectedJobName: "users.cleanup",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: null,
            onDuration: (value, _) => duration = value);

        var cleanupService = new ThrowingUserCleanupService();
        var options = Options.Create(new UserCleanupOptions { BatchSize = 10, RetentionDays = 30 });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new UserCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            tracker,
            NullLogger<UserCleanupJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());
        Assert.Equal(1, executionCount);
        Assert.Equal("failure", outcome);
        Assert.NotNull(duration);
        Assert.True(duration >= 0);
        Assert.Equal(1, tracker.GetSnapshot("users.cleanup")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("users.cleanup")?.LastFailedAtUtc);
    }

    [Fact]
    public async Task ImageCleanupJob_WithNonPositiveBatchSize_UsesOne() {
        var cleanupService = new RecordingImageCleanupService([1, 0]);
        var options = Options.Create(new ImageCleanupOptions { BatchSize = 0, OlderThanHours = 12 });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new ImageCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            new JobExecutionStateTracker(),
            NullLogger<ImageCleanupJob>.Instance);

        await job.Execute();

        Assert.Equal([1, 1], cleanupService.BatchSizes);
        Assert.Equal(now.AddHours(-12), cleanupService.OlderThanValues[0]);
    }

    [Fact]
    public async Task ImageCleanupJob_WithNonPositiveOlderThan_UsesDefault12Hours() {
        var cleanupService = new RecordingImageCleanupService([0]);
        var options = Options.Create(new ImageCleanupOptions { BatchSize = 10, OlderThanHours = 0 });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new ImageCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            new JobExecutionStateTracker(),
            NullLogger<ImageCleanupJob>.Instance);

        await job.Execute();

        Assert.Single(cleanupService.OlderThanValues);
        Assert.Equal(now.AddHours(-12), cleanupService.OlderThanValues[0]);
    }

    [Fact]
    public async Task UserCleanupJob_WithInvalidReassignUserId_PassesNull() {
        var cleanupService = new RecordingUserCleanupService([0]);
        var options = Options.Create(new UserCleanupOptions {
            BatchSize = 10,
            RetentionDays = 30,
            ReassignUserId = "not-a-guid",
        });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new UserCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            new JobExecutionStateTracker(),
            NullLogger<UserCleanupJob>.Instance);

        await job.Execute();

        Assert.Single(cleanupService.ReassignUserIds);
        Assert.Null(cleanupService.ReassignUserIds[0]);
        Assert.Equal(now.AddDays(-30), cleanupService.OlderThanValues[0]);
    }

    [Fact]
    public async Task UserCleanupJob_WithValidReassignUserId_PassesParsedGuid() {
        var expectedId = Guid.NewGuid();
        var cleanupService = new RecordingUserCleanupService([0]);
        var options = Options.Create(new UserCleanupOptions {
            BatchSize = 10,
            RetentionDays = 30,
            ReassignUserId = expectedId.ToString(),
        });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new UserCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            new JobExecutionStateTracker(),
            NullLogger<UserCleanupJob>.Instance);

        await job.Execute();

        Assert.Single(cleanupService.ReassignUserIds);
        Assert.Equal(expectedId, cleanupService.ReassignUserIds[0]);
    }

    [Fact]
    public async Task UserCleanupJob_WithNonPositiveBatchAndRetention_UsesDefaults() {
        var cleanupService = new RecordingUserCleanupService([1, 0]);
        var options = Options.Create(new UserCleanupOptions {
            BatchSize = 0,
            RetentionDays = 0,
        });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var job = new UserCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            new JobExecutionStateTracker(),
            NullLogger<UserCleanupJob>.Instance);

        await job.Execute();

        Assert.Equal([1, 1], cleanupService.BatchSizes);
        Assert.Equal(now.AddDays(-30), cleanupService.OlderThanValues[0]);
    }

    [Fact]
    public async Task NotificationCleanupJob_RecordsSuccessMetrics_AndBuildsExpectedPolicy() {
        long? executionCount = null;
        string? outcome = null;
        long? deletedItems = null;
        double? duration = null;

        using var listener = CreateJobManagerListener(
            expectedJobName: "notifications.cleanup",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: (value, _) => deletedItems = value,
            onDuration: (value, _) => duration = value);

        var cleanupService = new RecordingNotificationCleanupService([2, 0]);
        var options = Options.Create(new NotificationCleanupOptions {
            TransientTypes = ["Test", "Reminder"],
            BatchSize = 2,
            TransientReadRetentionDays = 3,
            TransientUnreadRetentionDays = 5,
            StandardReadRetentionDays = 14,
            StandardUnreadRetentionDays = 30,
        });
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new NotificationCleanupJob(
            cleanupService,
            options,
            new FixedDateTimeProvider(now),
            tracker,
            NullLogger<NotificationCleanupJob>.Instance);

        await job.Execute();

        Assert.Equal(1, executionCount);
        Assert.Equal("success", outcome);
        Assert.Equal(2, deletedItems);
        Assert.NotNull(duration);
        Assert.True(duration >= 0);
        Assert.Equal(0, tracker.GetSnapshot("notifications.cleanup")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("notifications.cleanup")?.LastSucceededAtUtc);
        Assert.Equal(2, cleanupService.Policies.Count);
        Assert.All(cleanupService.Policies, policy => {
            Assert.Equal(["Test", "Reminder"], policy.TransientTypes);
            Assert.Equal(3, policy.TransientReadRetentionDays);
            Assert.Equal(5, policy.TransientUnreadRetentionDays);
            Assert.Equal(14, policy.StandardReadRetentionDays);
            Assert.Equal(30, policy.StandardUnreadRetentionDays);
            Assert.Equal(2, policy.BatchSize);
        });
    }

    [Fact]
    public async Task NotificationCleanupJob_WhenCleanupFails_RecordsFailureMetric_AndRethrows() {
        long? executionCount = null;
        string? outcome = null;
        double? duration = null;

        using var listener = CreateJobManagerListener(
            expectedJobName: "notifications.cleanup",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: null,
            onDuration: (value, _) => duration = value);

        var cleanupService = new ThrowingNotificationCleanupService();
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new NotificationCleanupJob(
            cleanupService,
            Options.Create(new NotificationCleanupOptions { TransientTypes = ["Test"], BatchSize = 10 }),
            new FixedDateTimeProvider(now),
            tracker,
            NullLogger<NotificationCleanupJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute());

        Assert.Equal(1, executionCount);
        Assert.Equal("failure", outcome);
        Assert.NotNull(duration);
        Assert.True(duration >= 0);
        Assert.Equal(1, tracker.GetSnapshot("notifications.cleanup")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("notifications.cleanup")?.LastFailedAtUtc);
    }

    [Fact]
    public async Task RecurringJobsHostedService_StartAsync_RegistersExpectedJobs_AndVerifiesThem() {
        var recurringJobManager = new RecordingRecurringJobManager();
        var verifier = new RecordingRecurringJobRegistrationVerifier();
        var service = new RecurringJobsHostedService(
            recurringJobManager,
            verifier,
            Options.Create(new ImageCleanupOptions { Cron = "0 * * * *" }),
            Options.Create(new NotificationCleanupOptions {
                TransientTypes = ["Test"],
                Cron = "15 4 * * *",
            }),
            Options.Create(new UserCleanupOptions { Cron = "30 2 * * *" }),
            new ImageCleanupJob(
                new RecordingImageCleanupService([0]),
                Options.Create(new ImageCleanupOptions()),
                new FixedDateTimeProvider(DateTime.UtcNow),
                new JobExecutionStateTracker(),
                NullLogger<ImageCleanupJob>.Instance),
            new NotificationCleanupJob(
                new RecordingNotificationCleanupService([0]),
                Options.Create(new NotificationCleanupOptions { TransientTypes = ["Test"] }),
                new FixedDateTimeProvider(DateTime.UtcNow),
                new JobExecutionStateTracker(),
                NullLogger<NotificationCleanupJob>.Instance),
            new UserCleanupJob(
                new RecordingUserCleanupService([0]),
                Options.Create(new UserCleanupOptions()),
                new FixedDateTimeProvider(DateTime.UtcNow),
                new JobExecutionStateTracker(),
                NullLogger<UserCleanupJob>.Instance));

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(
            [RecurringJobIds.ImageAssetsCleanup, RecurringJobIds.NotificationsCleanup, RecurringJobIds.UsersCleanup],
            recurringJobManager.JobIds);
        Assert.Equal(
            [RecurringJobIds.ImageAssetsCleanup, RecurringJobIds.NotificationsCleanup, RecurringJobIds.UsersCleanup],
            verifier.ExpectedJobIds);
    }

    [Fact]
    public async Task RecurringJobsHostedService_StartAsync_WhenVerificationFails_Throws() {
        var recurringJobManager = new RecordingRecurringJobManager();
        var verifier = new ThrowingRecurringJobRegistrationVerifier();
        var service = new RecurringJobsHostedService(
            recurringJobManager,
            verifier,
            Options.Create(new ImageCleanupOptions { Cron = "0 * * * *" }),
            Options.Create(new NotificationCleanupOptions {
                TransientTypes = ["Test"],
                Cron = "15 4 * * *",
            }),
            Options.Create(new UserCleanupOptions { Cron = "30 2 * * *" }),
            new ImageCleanupJob(
                new RecordingImageCleanupService([0]),
                Options.Create(new ImageCleanupOptions()),
                new FixedDateTimeProvider(DateTime.UtcNow),
                new JobExecutionStateTracker(),
                NullLogger<ImageCleanupJob>.Instance),
            new NotificationCleanupJob(
                new RecordingNotificationCleanupService([0]),
                Options.Create(new NotificationCleanupOptions { TransientTypes = ["Test"] }),
                new FixedDateTimeProvider(DateTime.UtcNow),
                new JobExecutionStateTracker(),
                NullLogger<NotificationCleanupJob>.Instance),
            new UserCleanupJob(
                new RecordingUserCleanupService([0]),
                Options.Create(new UserCleanupOptions()),
                new FixedDateTimeProvider(DateTime.UtcNow),
                new JobExecutionStateTracker(),
                NullLogger<UserCleanupJob>.Instance));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void CleanupJobs_DeclareExpectedRetryAndConcurrencyPolicy() {
        var imageMethod = typeof(ImageCleanupJob).GetMethod(nameof(ImageCleanupJob.Execute));
        var notificationMethod = typeof(NotificationCleanupJob).GetMethod(nameof(NotificationCleanupJob.Execute));
        var userMethod = typeof(UserCleanupJob).GetMethod(nameof(UserCleanupJob.Execute));

        Assert.NotNull(imageMethod);
        Assert.NotNull(notificationMethod);
        Assert.NotNull(userMethod);

        AssertExecutionPolicy(imageMethod!);
        AssertExecutionPolicy(notificationMethod!);
        AssertExecutionPolicy(userMethod!);
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }

    private static MeterListener CreateJobManagerListener(
        string expectedJobName,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onExecution,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onDeletedItems,
        Action<double, ReadOnlySpan<KeyValuePair<string, object?>>>? onDuration) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (instrument.Meter.Name != JobManagerMeterName) {
                return;
            }

            if (instrument.Name is "fooddiary.job.execution.events" or "fooddiary.job.deleted_items" or "fooddiary.job.execution.duration") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (!string.Equals(GetTagValue(tags, "fooddiary.job.name"), expectedJobName, StringComparison.Ordinal)) {
                return;
            }

            if (instrument.Name == "fooddiary.job.execution.events") {
                onExecution?.Invoke(value, tags);
            } else if (instrument.Name == "fooddiary.job.deleted_items") {
                onDeletedItems?.Invoke(value, tags);
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => {
            if (!string.Equals(GetTagValue(tags, "fooddiary.job.name"), expectedJobName, StringComparison.Ordinal)) {
                return;
            }

            if (instrument.Name == "fooddiary.job.execution.duration") {
                onDuration?.Invoke(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (var tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private static void AssertExecutionPolicy(System.Reflection.MethodInfo method) {
        var retry = method.GetCustomAttributes(typeof(AutomaticRetryAttribute), inherit: false)
            .Cast<AutomaticRetryAttribute>()
            .SingleOrDefault();
        var concurrency = method.GetCustomAttributes(typeof(DisableConcurrentExecutionAttribute), inherit: false)
            .Cast<DisableConcurrentExecutionAttribute>()
            .SingleOrDefault();

        Assert.NotNull(retry);
        Assert.NotNull(concurrency);
        Assert.Equal(RecurringJobExecutionPolicy.CleanupRetryAttempts, retry!.Attempts);
        Assert.Equal(AttemptsExceededAction.Fail, retry.OnAttemptsExceeded);
        Assert.Equal(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds, concurrency!.TimeoutSec);
    }

    private sealed class RecordingImageCleanupService(IEnumerable<int> results) : IImageAssetCleanupService {
        private readonly Queue<int> _results = new(results);

        public List<int> BatchSizes { get; } = [];
        public List<DateTime> OlderThanValues { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(Domain.ValueObjects.Ids.ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeleteImageAssetResult(false));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) {
            OlderThanValues.Add(olderThanUtc);
            BatchSizes.Add(batchSize);
            var value = _results.Count > 0 ? _results.Dequeue() : 0;
            return Task.FromResult(value);
        }
    }

    private sealed class RecordingUserCleanupService(IEnumerable<int> results) : IUserCleanupService {
        private readonly Queue<int> _results = new(results);

        public List<int> BatchSizes { get; } = [];
        public List<DateTime> OlderThanValues { get; } = [];
        public List<Guid?> ReassignUserIds { get; } = [];

        public Task<int> CleanupDeletedUsersAsync(
            DateTime olderThanUtc,
            int batchSize,
            Guid? reassignUserId,
            CancellationToken cancellationToken = default) {
            OlderThanValues.Add(olderThanUtc);
            BatchSizes.Add(batchSize);
            ReassignUserIds.Add(reassignUserId);
            var value = _results.Count > 0 ? _results.Dequeue() : 0;
            return Task.FromResult(value);
        }
    }

    private sealed class RecordingNotificationCleanupService(IEnumerable<int> results) : INotificationCleanupService {
        private readonly Queue<int> _results = new(results);

        public List<NotificationCleanupPolicy> Policies { get; } = [];

        public Task<int> CleanupExpiredNotificationsAsync(NotificationCleanupPolicy policy, CancellationToken cancellationToken = default) {
            Policies.Add(policy);
            var value = _results.Count > 0 ? _results.Dequeue() : 0;
            return Task.FromResult(value);
        }
    }

    private sealed class ThrowingUserCleanupService : IUserCleanupService {
        public Task<int> CleanupDeletedUsersAsync(
            DateTime olderThanUtc,
            int batchSize,
            Guid? reassignUserId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("cleanup failed");
    }

    private sealed class ThrowingNotificationCleanupService : INotificationCleanupService {
        public Task<int> CleanupExpiredNotificationsAsync(NotificationCleanupPolicy policy, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("notification cleanup failed");
    }

    private sealed class RecordingRecurringJobManager : IRecurringJobManager {
        public List<string> JobIds { get; } = [];

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options) {
            JobIds.Add(recurringJobId);
        }

        public void Trigger(string recurringJobId) => throw new NotSupportedException();

        public void RemoveIfExists(string recurringJobId) => throw new NotSupportedException();
    }

    private sealed class RecordingRecurringJobRegistrationVerifier : IRecurringJobRegistrationVerifier {
        public List<string> ExpectedJobIds { get; } = [];

        public void EnsureRegistered(IReadOnlyCollection<string> expectedJobIds) {
            ExpectedJobIds.AddRange(expectedJobIds);
        }
    }

    private sealed class ThrowingRecurringJobRegistrationVerifier : IRecurringJobRegistrationVerifier {
        public void EnsureRegistered(IReadOnlyCollection<string> expectedJobIds) {
            throw new InvalidOperationException("verification failed");
        }
    }
}
