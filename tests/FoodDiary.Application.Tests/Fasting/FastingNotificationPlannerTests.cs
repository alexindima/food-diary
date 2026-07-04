using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using System.Globalization;

namespace FoodDiary.Application.Tests.Fasting;

[ExcludeFromCodeCoverage]
public sealed class FastingNotificationPlannerTests {
    private static readonly DateTime FixedNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CheckInReminderPlanner_WhenNoCheckInAndThresholdElapsed_ReturnsDueReferenceIds() {
        var user = User.Create("planner-reminders@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-21),
            sequenceNumber: 1,
            targetHours: 36);
        AttachNavigation(occurrence, plan, user);

        IReadOnlyList<string> referenceIds = FastingCheckInReminderPlanner.GetDueReferenceIds(
            occurrence,
            checkIns: null,
            FixedNow);

        Assert.Equal(
            [
                string.Create(CultureInfo.InvariantCulture, $"fasting-check-in-reminder:{occurrence.Id.Value}:{user.FastingCheckInReminderHours}"),
                string.Create(CultureInfo.InvariantCulture, $"fasting-check-in-reminder:{occurrence.Id.Value}:{user.FastingCheckInFollowUpReminderHours}"),
            ],
            referenceIds);
    }

    [Fact]
    public void CheckInReminderPlanner_WhenOccurrenceHasCheckIn_ReturnsNoReferenceIds() {
        var user = User.Create("planner-existing-check-in@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-21),
            sequenceNumber: 1,
            targetHours: 36);
        var checkIn = FastingCheckIn.Create(occurrence.Id, user.Id, 3, 3, 3, ["ok"], notes: null, FixedNow);
        AttachNavigation(occurrence, plan, user);

        IReadOnlyList<string> referenceIds = FastingCheckInReminderPlanner.GetDueReferenceIds(
            occurrence,
            [checkIn],
            FixedNow);

        Assert.Empty(referenceIds);
    }

    [Fact]
    public void CheckInReminderPlanner_WhenOccurrenceStartsInFuture_ReturnsNoReferenceIds() {
        var user = User.Create("planner-future-reminder@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddHours(1));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(1),
            sequenceNumber: 1,
            targetHours: 36);
        AttachNavigation(occurrence, plan, user);

        IReadOnlyList<string> referenceIds = FastingCheckInReminderPlanner.GetDueReferenceIds(
            occurrence,
            checkIns: null,
            FixedNow);

        Assert.Empty(referenceIds);
    }

    [Fact]
    public void IntermittentNotificationPlanner_WhenWindowsAreDue_ReturnsEatingAndFastingWindowPlans() {
        var user = User.Create("planner-intermittent@example.com", "hash");
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-2));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-25),
            sequenceNumber: 1,
            targetHours: 16);
        AttachNavigation(occurrence, plan, user);

        IReadOnlyList<FastingWindowNotificationPlan> plans =
            FastingIntermittentNotificationPlanner.GetDueNotifications(occurrence, plan, FixedNow);

        Assert.Collection(
            plans,
            first => {
                Assert.Equal(NotificationTypes.EatingWindowStarted, first.Type);
                Assert.Equal($"eating-window-started:{occurrence.Id.Value}:1", first.ReferenceId);
            },
            second => {
                Assert.Equal(NotificationTypes.FastingWindowStarted, second.Type);
                Assert.Equal($"fasting-window-started:{occurrence.Id.Value}:2", second.ReferenceId);
            });
    }

    [Fact]
    public void IntermittentNotificationPlanner_WhenWindowConfigMissing_ReturnsNoPlans() {
        var user = User.Create("planner-missing-window@example.com", "hash");
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-2));
        SetPrivateProperty<FastingPlan, int?>(plan, nameof(FastingPlan.IntermittentEatingWindowHours), value: null);
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-25),
            sequenceNumber: 1,
            targetHours: 16);
        AttachNavigation(occurrence, plan, user);

        IReadOnlyList<FastingWindowNotificationPlan> plans =
            FastingIntermittentNotificationPlanner.GetDueNotifications(occurrence, plan, FixedNow);

        Assert.Empty(plans);
    }

    [Fact]
    public void IntermittentNotificationPlanner_WhenOccurrenceStartsInFuture_ReturnsNoPlans() {
        var user = User.Create("planner-future-window@example.com", "hash");
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow.AddHours(1));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(1),
            sequenceNumber: 1,
            targetHours: 16);
        AttachNavigation(occurrence, plan, user);

        IReadOnlyList<FastingWindowNotificationPlan> plans =
            FastingIntermittentNotificationPlanner.GetDueNotifications(occurrence, plan, FixedNow);

        Assert.Empty(plans);
    }

    private static void AttachNavigation(FastingOccurrence occurrence, FastingPlan plan, User user) {
        SetPrivateProperty(occurrence, nameof(FastingOccurrence.Plan), plan);
        SetPrivateProperty(occurrence, nameof(FastingOccurrence.User), user);
    }

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value) {
        System.Reflection.PropertyInfo? property = typeof(TTarget).GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }
}
