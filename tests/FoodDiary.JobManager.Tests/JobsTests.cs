using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using FoodDiary.Application.Abstractions.Users.Common;
using System.Reflection;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class JobsTests {
    private const string JobManagerMeterName = "FoodDiary.JobManager";

    [Fact]
    public async Task ImageCleanupJob_RecordsSuccessMetrics() {
        long? executionCount = null;
        string? outcome = null;
        long? deletedItems = null;
        double? duration = null;

        using MeterListener listener = CreateJobManagerListener(
            expectedJobName: "images.cleanup",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: (value, _) => deletedItems = value,
            onDuration: (value, _) => duration = value);

        var cleanupService = new RecordingImageCleanupService([2, 0]);
        IOptions<ImageCleanupOptions> options = Options.Create(new ImageCleanupOptions { BatchSize = 2, OlderThanHours = 12 });
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

        using MeterListener listener = CreateJobManagerListener(
            expectedJobName: "users.cleanup",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: null,
            onDuration: (value, _) => duration = value);

        var cleanupService = new ThrowingUserCleanupService();
        IOptions<UserCleanupOptions> options = Options.Create(new UserCleanupOptions { BatchSize = 10, RetentionDays = 30 });
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
        IOptions<ImageCleanupOptions> options = Options.Create(new ImageCleanupOptions { BatchSize = 0, OlderThanHours = 12 });
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
        IOptions<ImageCleanupOptions> options = Options.Create(new ImageCleanupOptions { BatchSize = 10, OlderThanHours = 0 });
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
        IOptions<UserCleanupOptions> options = Options.Create(new UserCleanupOptions {
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
        IOptions<UserCleanupOptions> options = Options.Create(new UserCleanupOptions {
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
        IOptions<UserCleanupOptions> options = Options.Create(new UserCleanupOptions {
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

        using MeterListener listener = CreateJobManagerListener(
            expectedJobName: "notifications.cleanup",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: (value, _) => deletedItems = value,
            onDuration: (value, _) => duration = value);

        var cleanupService = new RecordingNotificationCleanupService([2, 0]);
        IOptions<NotificationCleanupOptions> options = Options.Create(new NotificationCleanupOptions {
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

        using MeterListener listener = CreateJobManagerListener(
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
    public async Task BillingRenewalJob_WhenDisabled_RecordsSuccessWithoutService() {
        long? executionCount = null;
        string? outcome = null;
        double? duration = null;

        using MeterListener listener = CreateJobManagerListener(
            expectedJobName: "billing.renewal",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: null,
            onDuration: (value, _) => duration = value);

        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new BillingRenewalJob(
            null!,
            Options.Create(new BillingRenewalOptions { Enabled = false }),
            new FixedDateTimeProvider(now),
            tracker,
            NullLogger<BillingRenewalJob>.Instance);

        await job.Execute();

        Assert.Equal(1, executionCount);
        Assert.Equal("success", outcome);
        Assert.NotNull(duration);
        Assert.True(duration >= 0);
        Assert.Equal(0, tracker.GetSnapshot("billing.renewal")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("billing.renewal")?.LastSucceededAtUtc);
    }

    [Fact]
    public async Task BillingRenewalJob_WhenProviderHasNoGateway_RecordsSuccess() {
        long? executionCount = null;
        string? outcome = null;
        double? duration = null;

        using MeterListener listener = CreateJobManagerListener(
            expectedJobName: "billing.renewal",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: null,
            onDuration: (value, _) => duration = value);

        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new BillingRenewalJob(
            CreateBillingRenewalServiceWithoutGateways(now),
            Options.Create(new BillingRenewalOptions {
                Enabled = true,
                Provider = "MissingProvider",
                BatchSize = 10
            }),
            new FixedDateTimeProvider(now),
            tracker,
            NullLogger<BillingRenewalJob>.Instance);

        await job.Execute();

        Assert.Equal(1, executionCount);
        Assert.Equal("success", outcome);
        Assert.NotNull(duration);
        Assert.True(duration >= 0);
        Assert.Equal(0, tracker.GetSnapshot("billing.renewal")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("billing.renewal")?.LastSucceededAtUtc);
    }

    [Fact]
    public async Task BillingRenewalJob_WhenRenewalsAreProcessed_RecordsSuccessMetric() {
        long? executionCount = null;
        string? outcome = null;
        double? duration = null;

        using MeterListener listener = CreateJobManagerListener(
            expectedJobName: "billing.renewal",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: null,
            onDuration: (value, _) => duration = value);

        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var user = User.Create("renewal-job@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_renewal_job",
            "pay_initial",
            "pm_initial",
            "active",
            now.AddMonths(-1),
            now.AddMinutes(-1),
            "evt_initial",
            now.AddMonths(-1));
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var tracker = new JobExecutionStateTracker();
        var service = new BillingRenewalService(
            subscriptionRepository,
            paymentRepository,
            userRepository,
            new NoOpBillingTransactionRunner(),
            [
                new FakeRecurringBillingGateway(
                    BillingProviderNames.YooKassa,
                    new BillingRecurringPaymentModel(
                        "pay_renewal_job",
                        "pm_renewal_job",
                        "price_monthly",
                        "monthly",
                        "active",
                        now,
                        now.AddMonths(1),
                        "evt_renewal_job",
                        7.99m,
                        "USD",
                        "{\"renewal\":true}"))
            ],
            new BillingAccessService(userRepository, subscriptionRepository, new FixedDateTimeProvider(now)),
            new FixedDateTimeProvider(now));
        var job = new BillingRenewalJob(
            service,
            Options.Create(new BillingRenewalOptions {
                Enabled = true,
                Provider = BillingProviderNames.YooKassa,
                BatchSize = 10
            }),
            new FixedDateTimeProvider(now),
            tracker,
            NullLogger<BillingRenewalJob>.Instance);

        await job.Execute();

        Assert.Equal(1, executionCount);
        Assert.Equal("success", outcome);
        Assert.NotNull(duration);
        Assert.True(duration >= 0);
        Assert.Equal(0, tracker.GetSnapshot("billing.renewal")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("billing.renewal")?.LastSucceededAtUtc);
        Assert.Equal("pay_renewal_job", subscription.ExternalSubscriptionId);
        Assert.Single(paymentRepository.Payments);
    }

    [Fact]
    public async Task BillingRenewalJob_WhenServiceThrows_RecordsFailureMetric_AndRethrows() {
        long? executionCount = null;
        string? outcome = null;
        double? duration = null;

        using MeterListener listener = CreateJobManagerListener(
            expectedJobName: "billing.renewal",
            onExecution: (value, tags) => {
                executionCount = value;
                outcome = GetTagValue(tags, "fooddiary.job.outcome");
            },
            onDeletedItems: null,
            onDuration: (value, _) => duration = value);

        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var tracker = new JobExecutionStateTracker();
        var job = new BillingRenewalJob(
            null!,
            Options.Create(new BillingRenewalOptions {
                Enabled = true,
                Provider = "YooKassa",
                BatchSize = 10
            }),
            new FixedDateTimeProvider(now),
            tracker,
            NullLogger<BillingRenewalJob>.Instance);

        await Assert.ThrowsAsync<NullReferenceException>(() => job.Execute());

        Assert.Equal(1, executionCount);
        Assert.Equal("failure", outcome);
        Assert.NotNull(duration);
        Assert.True(duration >= 0);
        Assert.Equal(1, tracker.GetSnapshot("billing.renewal")?.ConsecutiveFailures);
        Assert.Equal(now, tracker.GetSnapshot("billing.renewal")?.LastFailedAtUtc);
    }

    [Fact]
    public async Task RecurringJobsHostedService_StartAsync_RegistersExpectedJobs_AndVerifiesThem() {
        var recurringJobManager = new RecordingRecurringJobManager();
        var verifier = new RecordingRecurringJobRegistrationVerifier();
        var service = new RecurringJobsHostedService(
            recurringJobManager,
            verifier,
            Options.Create(new ImageCleanupOptions { Cron = "0 * * * *" }),
            Options.Create(new BillingRenewalOptions { Enabled = false, Cron = "15 * * * *" }),
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
            CreateBillingRenewalJob(),
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
            [
                RecurringJobIds.ImageAssetsCleanup,
                RecurringJobIds.BillingRenewal,
                RecurringJobIds.NotificationsCleanup,
                RecurringJobIds.UsersCleanup
            ],
            recurringJobManager.JobIds);
        Assert.Equal(
            [
                RecurringJobIds.ImageAssetsCleanup,
                RecurringJobIds.NotificationsCleanup,
                RecurringJobIds.UsersCleanup,
                RecurringJobIds.BillingRenewal
            ],
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
            Options.Create(new BillingRenewalOptions { Enabled = false, Cron = "15 * * * *" }),
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
            CreateBillingRenewalJob(),
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
    public async Task RecurringJobsHostedService_StopAsync_CompletesWithoutWork() {
        var service = new RecurringJobsHostedService(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void CleanupJobs_DeclareExpectedRetryAndConcurrencyPolicy() {
        MethodInfo? imageMethod = typeof(ImageCleanupJob).GetMethod(nameof(ImageCleanupJob.Execute));
        MethodInfo? billingRenewalMethod = typeof(BillingRenewalJob).GetMethod(nameof(BillingRenewalJob.Execute));
        MethodInfo? notificationMethod = typeof(NotificationCleanupJob).GetMethod(nameof(NotificationCleanupJob.Execute));
        MethodInfo? userMethod = typeof(UserCleanupJob).GetMethod(nameof(UserCleanupJob.Execute));

        Assert.NotNull(imageMethod);
        Assert.NotNull(billingRenewalMethod);
        Assert.NotNull(notificationMethod);
        Assert.NotNull(userMethod);

        AssertExecutionPolicy(imageMethod!);
        AssertExecutionPolicy(billingRenewalMethod!);
        AssertExecutionPolicy(notificationMethod!);
        AssertExecutionPolicy(userMethod!);
    }

    private static BillingRenewalJob CreateBillingRenewalJob() =>
        new(
            null!,
            Options.Create(new BillingRenewalOptions()),
            new FixedDateTimeProvider(DateTime.UtcNow),
            new JobExecutionStateTracker(),
            NullLogger<BillingRenewalJob>.Instance);

    private static BillingRenewalService CreateBillingRenewalServiceWithoutGateways(DateTime utcNow) =>
        new(
            null!,
            null!,
            null!,
            null!,
            Array.Empty<IBillingRecurringProviderGateway>(),
            null!,
            new FixedDateTimeProvider(utcNow));

    [ExcludeFromCodeCoverage]
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
            if (!string.Equals(instrument.Meter.Name, JobManagerMeterName, StringComparison.Ordinal)) {
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

            if (string.Equals(instrument.Name, "fooddiary.job.execution.events", StringComparison.Ordinal)) {
                onExecution?.Invoke(value, tags);
            } else if (string.Equals(instrument.Name, "fooddiary.job.deleted_items", StringComparison.Ordinal)) {
                onDeletedItems?.Invoke(value, tags);
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => {
            if (!string.Equals(GetTagValue(tags, "fooddiary.job.name"), expectedJobName, StringComparison.Ordinal)) {
                return;
            }

            if (string.Equals(instrument.Name, "fooddiary.job.execution.duration", StringComparison.Ordinal)) {
                onDuration?.Invoke(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (KeyValuePair<string, object?> tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private static void AssertExecutionPolicy(System.Reflection.MethodInfo method) {
        AutomaticRetryAttribute? retry = method.GetCustomAttributes(typeof(AutomaticRetryAttribute), inherit: false)
            .Cast<AutomaticRetryAttribute>()
            .SingleOrDefault();
        DisableConcurrentExecutionAttribute? concurrency = method.GetCustomAttributes(typeof(DisableConcurrentExecutionAttribute), inherit: false)
            .Cast<DisableConcurrentExecutionAttribute>()
            .SingleOrDefault();

        Assert.NotNull(retry);
        Assert.NotNull(concurrency);
        Assert.Equal(RecurringJobExecutionPolicy.CleanupRetryAttempts, retry!.Attempts);
        Assert.Equal(AttemptsExceededAction.Fail, retry.OnAttemptsExceeded);
        Assert.Equal(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds, concurrency!.TimeoutSec);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingImageCleanupService(IEnumerable<int> results) : IImageAssetCleanupService {
        private readonly Queue<int> _results = new(results);

        public List<int> BatchSizes { get; } = [];
        public List<DateTime> OlderThanValues { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(Domain.ValueObjects.Ids.ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeleteImageAssetResult(false));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) {
            OlderThanValues.Add(olderThanUtc);
            BatchSizes.Add(batchSize);
            int value = _results.Count > 0 ? _results.Dequeue() : 0;
            return Task.FromResult(value);
        }
    }

    [ExcludeFromCodeCoverage]
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
            int value = _results.Count > 0 ? _results.Dequeue() : 0;
            return Task.FromResult(value);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationCleanupService(IEnumerable<int> results) : INotificationCleanupService {
        private readonly Queue<int> _results = new(results);

        public List<NotificationCleanupPolicy> Policies { get; } = [];

        public Task<int> CleanupExpiredNotificationsAsync(NotificationCleanupPolicy policy, CancellationToken cancellationToken = default) {
            Policies.Add(policy);
            int value = _results.Count > 0 ? _results.Dequeue() : 0;
            return Task.FromResult(value);
        }
    }

    private static BillingSubscription CreateSubscriptionSnapshot(
        User user,
        string provider,
        string externalCustomerId,
        string? externalSubscriptionId,
        string? externalPaymentMethodId,
        string status,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        string eventId,
        DateTime eventCreatedAtUtc) {
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            provider,
            externalCustomerId,
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            provider,
            externalSubscriptionId,
            externalPaymentMethodId,
            "price_monthly",
            "monthly",
            status,
            periodStartUtc,
            periodEndUtc,
            false,
            null,
            null,
            null,
            eventId,
            eventCreatedAtUtc);
        return subscription;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeUserRepository(params User[] users) : IUserRepository {
        private readonly List<User> _users = users.ToList();
        private readonly Role _premiumRole = Role.Create(RoleNames.Premium);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user =>
                IsAccessible(user) &&
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => IsAccessible(user) && user.Id == id));

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(
            long telegramUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search,
            int page,
            int limit,
            bool includeDeleted,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<User> Items, int TotalItems)>((_users, _users.Count));

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
            GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            Task.FromResult((_users.Count, _users.Count, 0, 0, (IReadOnlyList<User>)_users.Take(recentLimit).ToList()));

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<Role> roles = names.Contains(RoleNames.Premium, StringComparer.Ordinal)
                ? [_premiumRole]
                : [];
            return Task.FromResult(roles);
        }

        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) {
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        private static bool IsAccessible(User user) => user is { IsActive: true, DeletedAt: null };
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryBillingSubscriptionRepository(params BillingSubscription[] subscriptions)
        : IBillingSubscriptionRepository {
        public List<BillingSubscription> Subscriptions { get; } = subscriptions.ToList();

        public Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription => subscription.UserId == userId));

        public Task<BillingSubscription?> GetByExternalCustomerIdAsync(
            string provider,
            string externalCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalCustomerId, externalCustomerId, StringComparison.Ordinal)));

        public Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(
            string provider,
            string externalSubscriptionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalSubscriptionId, externalSubscriptionId, StringComparison.Ordinal)));

        public Task<BillingSubscription?> GetByExternalPaymentMethodIdAsync(
            string provider,
            string externalPaymentMethodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalPaymentMethodId, externalPaymentMethodId, StringComparison.Ordinal)));

        public Task<IReadOnlyList<BillingSubscription>> GetDueForRenewalAsync(
            string provider,
            DateTime dueAtUtc,
            int limit,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<BillingSubscription> dueSubscriptions = Subscriptions
                .Where(subscription =>
                    string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                    subscription.NextBillingAttemptUtc.HasValue &&
                    subscription.NextBillingAttemptUtc <= dueAtUtc)
                .Take(limit)
                .ToList();
            return Task.FromResult(dueSubscriptions);
        }

        public Task<BillingSubscription> AddAsync(
            BillingSubscription subscription,
            CancellationToken cancellationToken = default) {
            Subscriptions.Add(subscription);
            return Task.FromResult(subscription);
        }

        public Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingBillingPaymentRepository : IBillingPaymentRepository {
        public List<BillingPayment> Payments { get; } = [];

        public Task<BillingPayment?> GetByExternalPaymentIdAsync(
            string provider,
            string externalPaymentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.FirstOrDefault(payment =>
                string.Equals(payment.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(payment.ExternalPaymentId, externalPaymentId, StringComparison.Ordinal)));

        public Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default) {
            Payments.Add(payment);
            return Task.FromResult(payment);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeRecurringBillingGateway(
        string provider,
        BillingRecurringPaymentModel renewal)
        : IBillingRecurringProviderGateway {
        public string Provider { get; } = provider;

        public Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
            BillingRecurringPaymentRequestModel request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(renewal));
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoOpBillingTransactionRunner : IBillingTransactionRunner {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingUserCleanupService : IUserCleanupService {
        public Task<int> CleanupDeletedUsersAsync(
            DateTime olderThanUtc,
            int batchSize,
            Guid? reassignUserId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("cleanup failed");
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingNotificationCleanupService : INotificationCleanupService {
        public Task<int> CleanupExpiredNotificationsAsync(NotificationCleanupPolicy policy, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("notification cleanup failed");
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingRecurringJobManager : IRecurringJobManager {
        public List<string> JobIds { get; } = [];

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options) {
            JobIds.Add(recurringJobId);
        }

        public void Trigger(string recurringJobId) => throw new NotSupportedException();

        public void RemoveIfExists(string recurringJobId) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingRecurringJobRegistrationVerifier : IRecurringJobRegistrationVerifier {
        public List<string> ExpectedJobIds { get; } = [];

        public void EnsureRegistered(IReadOnlyCollection<string> expectedJobIds) {
            ExpectedJobIds.AddRange(expectedJobIds);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingRecurringJobRegistrationVerifier : IRecurringJobRegistrationVerifier {
        public void EnsureRegistered(IReadOnlyCollection<string> expectedJobIds) {
            throw new InvalidOperationException("verification failed");
        }
    }
}
