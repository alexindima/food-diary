using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationContractTests {
    [Fact]
    public void NotificationPayloadSerializer_Deserialize_WithEmptyPayload_ReturnsDefault() {
        NewRecommendationNotificationPayload? payload = NotificationPayloadSerializer.Deserialize<NewRecommendationNotificationPayload>(" ");

        Assert.Null(payload);
    }

    [Fact]
    public void NotificationPayloadSerializer_TryDeserialize_WithEmptyPayload_ReturnsFalse() {
        bool success = NotificationPayloadSerializer.TryDeserialize<NewRecommendationNotificationPayload>(" ", out NewRecommendationNotificationPayload? payload);

        Assert.False(success);
        Assert.Null(payload);
    }

    [Fact]
    public void NotificationPayloadSerializer_TryDeserialize_WithInvalidPayload_ReturnsFalse() {
        bool success = NotificationPayloadSerializer.TryDeserialize<NewRecommendationNotificationPayload>("{", out NewRecommendationNotificationPayload? payload);

        Assert.False(success);
        Assert.Null(payload);
    }

    [Fact]
    public void NotificationPayloadSerializer_TryDeserialize_WithValidPayload_ReturnsPayload() {
        string json = NotificationPayloads.NewRecommendation("Anna");

        bool success = NotificationPayloadSerializer.TryDeserialize<NewRecommendationNotificationPayload>(json, out NewRecommendationNotificationPayload? payload);

        Assert.True(success);
        Assert.NotNull(payload);
        Assert.Equal("Anna", payload.DietologistName);
    }

    [Theory]
    [InlineData(NotificationTypes.PasswordSetupSuggested, null, "/profile?intent=set-password")]
    [InlineData(NotificationTypes.FastingCheckInReminder, null, "/fasting?intent=check-in")]
    [InlineData(NotificationTypes.FastingCompleted, null, "/fasting?intent=session-complete")]
    [InlineData(NotificationTypes.FastingWindowStarted, null, "/fasting?intent=fasting-window")]
    [InlineData(NotificationTypes.EatingWindowStarted, null, "/fasting?intent=eating-window")]
    [InlineData(NotificationTypes.NewRecommendation, null, "/recommendations")]
    [InlineData(NotificationTypes.NewRecommendation, "recommendation-id", "/recommendations?recommendationId=recommendation-id")]
    [InlineData(NotificationTypes.NewRecommendationComment, null, "/recommendations")]
    [InlineData(NotificationTypes.NewRecommendationComment, "comment-recommendation-id", "/recommendations?recommendationId=comment-recommendation-id")]
    [InlineData(NotificationTypes.NewClientTask, null, "/recommendations")]
    [InlineData(NotificationTypes.ClientTaskCancelled, null, "/recommendations")]
    [InlineData(NotificationTypes.ClientTaskDueSoon, null, "/recommendations")]
    [InlineData(NotificationTypes.WeeklyGoalReminder, null, "/weekly-check-in")]
    [InlineData(NotificationTypes.ClientTaskChangedForDietologist, null, null)]
    [InlineData(NotificationTypes.ClientTaskChangedForDietologist, "client-id", "/dietologist/clients/client-id")]
    [InlineData(NotificationTypes.NewRecommendationCommentForDietologist, "bad", null)]
    [InlineData(NotificationTypes.NewRecommendationCommentForDietologist, null, null)]
    [InlineData(NotificationTypes.NewRecommendationCommentForDietologist, "bad|also-bad", null)]
    [InlineData(NotificationTypes.NewRecommendationCommentForDietologist, "00000000-0000-0000-0000-000000000001|bad", null)]
    [InlineData(
        NotificationTypes.NewRecommendationCommentForDietologist,
        "00000000-0000-0000-0000-000000000001|00000000-0000-0000-0000-000000000002",
        "/dietologist/clients/00000000-0000-0000-0000-000000000001?recommendationId=00000000-0000-0000-0000-000000000002")]
    [InlineData(NotificationTypes.DietologistInvitationReceived, "invitation-id", "/dietologist-invitations/invitation-id")]
    [InlineData(NotificationTypes.DietologistInvitationReceived, null, null)]
    [InlineData(NotificationTypes.DietologistInvitationAccepted, null, "/profile")]
    [InlineData(NotificationTypes.DietologistInvitationDeclined, null, "/profile")]
    [InlineData("Unknown", null, null)]
    public void NotificationTargetUrlResolver_Resolve_ReturnsExpectedUrl(
        string notificationType,
        string? referenceId,
        string? expectedUrl) {
        string? url = NotificationTargetUrlResolver.Resolve(notificationType, referenceId);

        Assert.Equal(expectedUrl, url);
    }

    [Theory]
    [InlineData("not-a-guid|00000000-0000-0000-0000-000000000001")]
    [InlineData("00000000-0000-0000-0000-000000000001|not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public void NotificationTargetUrlResolver_ForMalformedDietologistCommentReference_ReturnsNull(string referenceId) {
        string? url = NotificationTargetUrlResolver.Resolve(
            NotificationTypes.NewRecommendationCommentForDietologist,
            referenceId);

        Assert.Null(url);
    }

    [Fact]
    public void WebPushClientConfiguration_StoresOptions() {
        var configuration = new WebPushClientConfiguration(Enabled: true, PublicKey: "public-key");

        Assert.True(configuration.Enabled);
        Assert.Equal("public-key", configuration.PublicKey);
    }

    [Fact]
    public void WebPushSubscriptionData_StoresSubscriptionDetails() {
        var expirationTimeUtc = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Utc);

        var data = new WebPushSubscriptionData(
            Endpoint: "https://push.example.test/subscription",
            P256Dh: "p256dh",
            Auth: "auth",
            ExpirationTimeUtc: expirationTimeUtc,
            Locale: "en",
            UserAgent: "Test Agent");

        Assert.Equal("https://push.example.test/subscription", data.Endpoint);
        Assert.Equal("p256dh", data.P256Dh);
        Assert.Equal("auth", data.Auth);
        Assert.Equal(expirationTimeUtc, data.ExpirationTimeUtc);
        Assert.Equal("en", data.Locale);
        Assert.Equal("Test Agent", data.UserAgent);
    }
}
