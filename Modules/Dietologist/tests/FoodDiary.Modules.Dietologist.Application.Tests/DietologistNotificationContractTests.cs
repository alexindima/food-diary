using System.Text.Json;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistNotificationContractTests {
    [Theory]
    [InlineData("FastingCompleted")]
    [InlineData("EatingWindowStarted")]
    [InlineData("FastingWindowStarted")]
    public void FastingNotifications_PreserveRecipientOccurrenceAndTypedPayloadWireFields(string type) {
        var user = UserId.New();
        NotificationRequest request = type switch {
            "FastingCompleted" => DietologistNotificationFactory.CreateFastingCompleted(user, "Cyclic", "FastDay", "occurrence-123"),
            "EatingWindowStarted" => DietologistNotificationFactory.CreateEatingWindowStarted(user, "Cyclic", "FastDay", "occurrence-123"),
            _ => DietologistNotificationFactory.CreateFastingWindowStarted(user, "Cyclic", "FastDay", "occurrence-123"),
        };
        using var payload = JsonDocument.Parse(request.PayloadJson);

        Assert.Multiple(
            () => Assert.Equal(user, request.UserId),
            () => Assert.Equal(type, request.Type),
            () => Assert.Equal("occurrence-123", request.ReferenceId),
            () => Assert.False(request.Intent.IsLegacy),
            () => Assert.Equal("Cyclic", payload.RootElement.GetProperty("planType").GetString()),
            () => Assert.Equal("FastDay", payload.RootElement.GetProperty("occurrenceKind").GetString()),
            () => Assert.Equal(2, payload.RootElement.EnumerateObject().Count()));
    }

    [Theory]
    [InlineData("PasswordSetupSuggested", "security-version-12")]
    [InlineData("WeeklyGoalReminder", "goal-123")]
    [InlineData("FastingCheckInReminder", "occurrence-123")]
    [InlineData("NewComment", "comment-123")]
    [InlineData("NewComment", null)]
    public void ReferenceOnlyNotifications_PreserveTheReferenceAndUseAnEmptyTypedPayload(string type, string? reference) {
        var user = UserId.New();
        NotificationRequest request = type switch {
            "PasswordSetupSuggested" => DietologistNotificationFactory.CreatePasswordSetupSuggested(user, reference!),
            "WeeklyGoalReminder" => DietologistNotificationFactory.CreateWeeklyGoalReminder(user, reference!),
            "FastingCheckInReminder" => DietologistNotificationFactory.CreateFastingCheckInReminder(user, reference!),
            _ => DietologistNotificationFactory.CreateNewComment(user, reference),
        };

        Assert.Multiple(
            () => Assert.Equal(user, request.UserId),
            () => Assert.Equal(type, request.Type),
            () => Assert.Equal(reference, request.ReferenceId),
            () => Assert.Equal("{}", request.PayloadJson),
            () => Assert.False(request.Intent.IsLegacy));
    }
}
