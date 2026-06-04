using FluentValidation.TestHelper;
using FoodDiary.Application.Notifications.Commands.ScheduleTestNotification;
using FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;
using FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;

namespace FoodDiary.Application.Tests.Notifications;

[ExcludeFromCodeCoverage]
public sealed class NotificationsValidatorTests {
    [Fact]
    public async Task UpsertWebPushSubscription_WithRequiredFieldsMissing_HasErrors() {
        var validator = new UpsertWebPushSubscriptionCommandValidator();

        var result = await validator.TestValidateAsync(
            new UpsertWebPushSubscriptionCommand(null, "", "", "", null, null, null));

        result.ShouldHaveValidationErrorFor(command => command.UserId);
        result.ShouldHaveValidationErrorFor(command => command.Endpoint);
        result.ShouldHaveValidationErrorFor(command => command.P256Dh);
        result.ShouldHaveValidationErrorFor(command => command.Auth);
    }

    [Fact]
    public async Task UpsertWebPushSubscription_WithLongOptionalFields_HasErrors() {
        var validator = new UpsertWebPushSubscriptionCommandValidator();

        var result = await validator.TestValidateAsync(
            new UpsertWebPushSubscriptionCommand(
                Guid.NewGuid(),
                new string('e', 2049),
                new string('p', 513),
                new string('a', 513),
                null,
                new string('l', 17),
                new string('u', 513)));

        result.ShouldHaveValidationErrorFor(command => command.Endpoint);
        result.ShouldHaveValidationErrorFor(command => command.P256Dh);
        result.ShouldHaveValidationErrorFor(command => command.Auth);
        result.ShouldHaveValidationErrorFor(command => command.Locale);
        result.ShouldHaveValidationErrorFor(command => command.UserAgent);
    }

    [Fact]
    public async Task UpsertWebPushSubscription_WithValidInput_HasNoErrors() {
        var validator = new UpsertWebPushSubscriptionCommandValidator();

        var result = await validator.TestValidateAsync(
            new UpsertWebPushSubscriptionCommand(
                Guid.NewGuid(),
                "https://push.example/sub",
                "p256dh",
                "auth",
                null,
                "en",
                "Browser"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WithInvalidReminderHours_HasErrors() {
        var validator = new UpdateNotificationPreferencesCommandValidator();

        var result = await validator.TestValidateAsync(
            new UpdateNotificationPreferencesCommand(Guid.NewGuid(), null, null, null, 0, 169));

        result.ShouldHaveValidationErrorFor(command => command.FastingCheckInReminderHours);
        result.ShouldHaveValidationErrorFor(command => command.FastingCheckInFollowUpReminderHours);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WithFollowUpBeforeFirstReminder_HasError() {
        var validator = new UpdateNotificationPreferencesCommandValidator();

        var result = await validator.TestValidateAsync(
            new UpdateNotificationPreferencesCommand(Guid.NewGuid(), null, null, null, 12, 12));

        result.ShouldHaveValidationErrorFor(command => command);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WithValidInput_HasNoErrors() {
        var validator = new UpdateNotificationPreferencesCommandValidator();

        var result = await validator.TestValidateAsync(
            new UpdateNotificationPreferencesCommand(Guid.NewGuid(), true, true, false, 12, 18));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ScheduleTestNotification_WithEmptyUserId_HasError() {
        var validator = new ScheduleTestNotificationCommandValidator();

        var result = await validator.TestValidateAsync(
            new ScheduleTestNotificationCommand(null, 15, "test"));

        result.ShouldHaveValidationErrorFor(command => command.UserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    public async Task ScheduleTestNotification_WithInvalidDelay_HasError(int delaySeconds) {
        var validator = new ScheduleTestNotificationCommandValidator();

        var result = await validator.TestValidateAsync(
            new ScheduleTestNotificationCommand(Guid.NewGuid(), delaySeconds, "test"));

        result.ShouldHaveValidationErrorFor(command => command.DelaySeconds);
    }

    [Fact]
    public async Task ScheduleTestNotification_WithValidInput_HasNoErrors() {
        var validator = new ScheduleTestNotificationCommandValidator();

        var result = await validator.TestValidateAsync(
            new ScheduleTestNotificationCommand(Guid.NewGuid(), 60, "test"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
