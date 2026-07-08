using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace FoodDiary.Application.Tests.Fasting;

public partial class FastingFeatureTests {
    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenNoActiveOccurrences_ReturnsZeroWithoutPushes() {
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenPostCommitQueueHasActions_FlushesQueue() {
        var postCommitActionQueue = new RecordingPostCommitActionQueue(hasActions: true);
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(),
            new InMemoryFastingCheckInRepository(),
            new InMemorySchedulerNotificationRepository(),
            new InMemorySchedulerNotificationRepository(),
            new InMemorySchedulerNotificationWriter(new InMemorySchedulerNotificationRepository(), new RecordingWebPushNotificationSender()),
            new RecordingNotificationPusher(),
            CreateUnitOfWork(),
            postCommitActionQueue,
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Equal(1, postCommitActionQueue.FlushCallCount);
    }

    [Fact]
    public void FastingNotificationFactory_WithUnsupportedNotificationType_Throws() {
        var user = User.Create("fasting-unsupported-notification@example.com", "hash");
        var candidate = new FastingNotificationCandidate(
            user.Id,
            "unsupported",
            "unsupported-reference",
            "Cyclic",
            "FastDay");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => FastingNotificationFactory.Create(candidate));
        Assert.Contains("Unsupported fasting notification type", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenPlanIsPaused_SkipsOccurrence() {
        var user = User.Create("fasting-paused-plan@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        plan.Pause();
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-40), 1, 36);
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenOccurrenceHasNoPlan_SkipsOccurrence() {
        var user = User.Create("fasting-missing-plan@example.com", "hash");
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-40), 1, 36);
        SetPrivateProperty(occurrence, nameof(FastingOccurrence.User), user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenOccurrenceHasRealCheckIn_SuppressesCheckInReminder() {
        var user = User.Create("fasting-notifications@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-13), 1, 36);
        AttachNavigation(occurrence, plan, user);

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(occurrence);
        var checkInRepo = new InMemoryFastingCheckInRepository(
            FastingCheckIn.Create(occurrence.Id, user.Id, 2, 4, 4, ["weakness"], "steady", FixedNow.AddHours(-1)));
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            occurrenceRepo,
            checkInRepo,
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenOccurrenceStartsInFuture_SkipsReminder() {
        var user = User.Create("fasting-future-reminder@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddHours(1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(1), 1, 36);
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenOnlyLegacySummaryCheckInExists_SuppressesCheckInReminder() {
        var user = User.Create("fasting-summary@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-13), 1, 36);
        occurrence.UpdateCheckIn(2, 4, 4, ["weakness"], "legacy", FixedNow.AddHours(-2));
        AttachNavigation(occurrence, plan, user);

        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenNoCheckInExists_CreatesReminderAndPushesUnreadCount() {
        var user = User.Create("fasting-reminder@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-13), 1, 36);
        AttachNavigation(occurrence, plan, user);

        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, created);
        Assert.Single(notificationRepo.Stored);
        Assert.Single(webPushSender.Sent);
        Assert.Single(notificationPusher.UnreadCountUsers);
        Assert.Single(notificationPusher.ChangedUsers);
        Assert.Equal(user.Id.Value, notificationPusher.UnreadCountUsers[0]);
        Assert.Equal(user.Id.Value, notificationPusher.ChangedUsers[0]);
        Assert.Equal(NotificationTypes.FastingCheckInReminder, notificationRepo.Stored[0].Type);
        Assert.Equal(string.Create(CultureInfo.InvariantCulture, $"fasting-check-in-reminder:{occurrence.Id.Value}:{user.FastingCheckInReminderHours}"), notificationRepo.Stored[0].ReferenceId);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenPastFollowUpThreshold_CreatesTwoRemindersOnceAndDeduplicatesLaterRuns() {
        var user = User.Create("fasting-reminder-thresholds@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-21), 1, 36);
        AttachNavigation(occurrence, plan, user);

        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int firstCreated = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);
        int secondCreated = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(2, firstCreated);
        Assert.Equal(0, secondCreated);
        Assert.Equal(2, notificationRepo.Stored.Count);
        Assert.Equal(2, webPushSender.Sent.Count);
        Assert.Contains(notificationRepo.Stored, x => string.Equals(x.ReferenceId, string.Create(CultureInfo.InvariantCulture, $"fasting-check-in-reminder:{occurrence.Id.Value}:{user.FastingCheckInReminderHours}"), StringComparison.Ordinal));
        Assert.Contains(notificationRepo.Stored, x => string.Equals(x.ReferenceId, string.Create(CultureInfo.InvariantCulture, $"fasting-check-in-reminder:{occurrence.Id.Value}:{user.FastingCheckInFollowUpReminderHours}"), StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenExtendedTargetElapsed_CreatesCompletionNotification() {
        var user = User.Create("fasting-completion@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-2));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-37), 1, 36);
        occurrence.UpdateCheckIn(2, 4, 4, ["ok"], "checked", FixedNow.AddHours(-1));
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, created);
        Notification notification = Assert.Single(notificationRepo.Stored);
        Assert.Equal(NotificationTypes.FastingCompleted, notification.Type);
        Assert.Equal($"fasting-completed:{occurrence.Id.Value}", notification.ReferenceId);
        Assert.Single(webPushSender.Sent);
        Assert.Single(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenExtendedTargetMissing_SkipsCompletionNotification() {
        var user = User.Create("fasting-no-target@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-2));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-37), 1, targetHours: null);
        occurrence.UpdateCheckIn(2, 4, 4, ["ok"], "checked", FixedNow.AddHours(-1));
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenCompletionNotificationAlreadyExists_SkipsDuplicate() {
        var user = User.Create("fasting-completion-duplicate@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-2));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-37), 1, 36);
        occurrence.UpdateCheckIn(2, 4, 4, ["ok"], "checked", FixedNow.AddHours(-1));
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        notificationRepo.Stored.Add(NotificationFactory.CreateFastingCompleted(
            user.Id,
            plan.Type.ToString(),
            occurrence.Kind.ToString(),
            $"fasting-completed:{occurrence.Id.Value}"));
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Single(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenIntermittentWindowsAreDue_CreatesWindowNotifications() {
        var user = User.Create("fasting-intermittent@example.com", "hash");
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-2));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-25), 1, 16);
        occurrence.UpdateCheckIn(2, 4, 4, ["ok"], "checked", FixedNow.AddHours(-1));
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(2, created);
        Assert.Contains(notificationRepo.Stored, x =>
            string.Equals(x.Type, NotificationTypes.EatingWindowStarted, StringComparison.Ordinal) &&
            string.Equals(x.ReferenceId, $"eating-window-started:{occurrence.Id.Value}:1", StringComparison.Ordinal));
        Assert.Contains(notificationRepo.Stored, x =>
            string.Equals(x.Type, NotificationTypes.FastingWindowStarted, StringComparison.Ordinal) &&
            string.Equals(x.ReferenceId, $"fasting-window-started:{occurrence.Id.Value}:2", StringComparison.Ordinal));
        Assert.Equal(2, webPushSender.Sent.Count);
        Assert.Single(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenIntermittentWindowConfigMissing_SkipsWindowNotifications() {
        var user = User.Create("fasting-intermittent-missing-window@example.com", "hash");
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-2));
        SetPrivateProperty<FastingPlan, int?>(plan, nameof(FastingPlan.IntermittentEatingWindowHours), value: null);
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-25), 1, 16);
        occurrence.UpdateCheckIn(2, 4, 4, ["ok"], "checked", FixedNow.AddHours(-1));
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenIntermittentOccurrenceStartsInFuture_SkipsWindowNotifications() {
        var user = User.Create("fasting-intermittent-future@example.com", "hash");
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow.AddHours(1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(1), 1, 16);
        occurrence.UpdateCheckIn(2, 4, 4, ["ok"], "checked", FixedNow);
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenIntermittentWindowNotificationAlreadyExists_SkipsDuplicate() {
        var user = User.Create("fasting-intermittent-duplicate@example.com", "hash");
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-17), 1, 16);
        occurrence.UpdateCheckIn(2, 4, 4, ["ok"], "checked", FixedNow.AddHours(-1));
        AttachNavigation(occurrence, plan, user);
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        notificationRepo.Stored.Add(NotificationFactory.CreateEatingWindowStarted(
            user.Id,
            plan.Type.ToString(),
            occurrence.Kind.ToString(),
            $"eating-window-started:{occurrence.Id.Value}:1"));
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationRepo,
            new InMemorySchedulerNotificationWriter(notificationRepo, webPushSender),
            notificationPusher,
            CreateUnitOfWork(),
            new ImmediatePostCommitActionQueue(),
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        int created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Single(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }
}
