using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationFactoryTests {
    [Fact]
    public void FastingWindowStarted_PreservesTheOwnerPhasePayloadAndOccurrenceReference() {
        var userId = UserId.New();

        Notification notification = NotificationFactory.CreateFastingWindowStarted(userId, "cyclic", "fasting", "occurrence-id");
        FastingPhaseNotificationPayload? payload = NotificationPayloadSerializer.Deserialize<FastingPhaseNotificationPayload>(notification.PayloadJson);

        Assert.NotNull(payload);
        Assert.Multiple(
            () => Assert.Equal(userId, notification.UserId),
            () => Assert.Equal(NotificationTypes.FastingWindowStarted, notification.Type),
            () => Assert.Equal("occurrence-id", notification.ReferenceId),
            () => Assert.Equal("cyclic", payload.PlanType),
            () => Assert.Equal("fasting", payload.OccurrenceKind));
    }

    [Fact]
    public void FastingCheckInReminder_PreservesTheOccurrenceReferenceAndEmptyPayload() {
        var userId = UserId.New();

        Notification notification = NotificationFactory.CreateFastingCheckInReminder(userId, "occurrence-id");

        Assert.Multiple(
            () => Assert.Equal(userId, notification.UserId),
            () => Assert.Equal(NotificationTypes.FastingCheckInReminder, notification.Type),
            () => Assert.Equal("occurrence-id", notification.ReferenceId),
            () => Assert.Equal("{}", notification.PayloadJson));
    }

    [Fact]
    public void RecommendationCommentForRecipient_LinksDirectlyToTheRecommendation() {
        var userId = UserId.New();

        Notification notification = NotificationFactory.CreateNewRecommendationComment(userId, "recommendation-id", "client-id", forDietologist: false);

        Assert.Multiple(
            () => Assert.Equal(userId, notification.UserId),
            () => Assert.Equal(NotificationTypes.NewRecommendationComment, notification.Type),
            () => Assert.Equal("recommendation-id", notification.ReferenceId),
            () => Assert.Equal("{}", notification.PayloadJson));
    }

    [Theory]
    [InlineData(false, false, NotificationTypes.NewClientTask, null)]
    [InlineData(true, false, NotificationTypes.ClientTaskChangedForDietologist, "client-id")]
    [InlineData(false, true, NotificationTypes.ClientTaskCancelled, null)]
    [InlineData(true, true, NotificationTypes.ClientTaskCancelled, "client-id")]
    public void CreateClientTaskChanged_MapsAudienceAndCancellation(
        bool forDietologist,
        bool cancelled,
        string expectedType,
        string? expectedReferenceId) {
        Notification notification = NotificationFactory.CreateClientTaskChanged(
            UserId.New(), "client-id", forDietologist, cancelled);

        Assert.Multiple(
            () => Assert.Equal(expectedType, notification.Type),
            () => Assert.Equal(expectedReferenceId, notification.ReferenceId));
    }

    [Fact]
    public void PreviouslyUncoveredFactories_MapTypePayloadAndReference() {
        var userId = UserId.New();

        Notification recommendation = NotificationFactory.CreateNewRecommendation(userId, "Dietologist", "recommendation-id");
        Notification recommendationComment = NotificationFactory.CreateNewRecommendationComment(
            userId, "recommendation-id", "client-id", forDietologist: true);
        Notification comment = NotificationFactory.CreateNewComment(userId, "comment-id");
        Notification weeklyGoal = NotificationFactory.CreateWeeklyGoalReminder(userId, "goal-id");

        Assert.Multiple(
            () => Assert.Equal(NotificationTypes.NewRecommendation, recommendation.Type),
            () => Assert.Equal("recommendation-id", recommendation.ReferenceId),
            () => Assert.Equal(NotificationTypes.NewRecommendationCommentForDietologist, recommendationComment.Type),
            () => Assert.Equal("client-id|recommendation-id", recommendationComment.ReferenceId),
            () => Assert.Equal(NotificationTypes.NewComment, comment.Type),
            () => Assert.Equal("comment-id", comment.ReferenceId),
            () => Assert.Equal(NotificationTypes.WeeklyGoalReminder, weeklyGoal.Type),
            () => Assert.Equal("goal-id", weeklyGoal.ReferenceId));
    }

    [Fact]
    public void InvitationAndDueSoonFactories_MapTypesAndReferences() {
        var userId = UserId.New();

        Notification received = NotificationFactory.CreateDietologistInvitationReceived(userId, "Client", "received-id");
        Notification accepted = NotificationFactory.CreateDietologistInvitationAccepted(userId, "Dietologist", "accepted-id");
        Notification declined = NotificationFactory.CreateDietologistInvitationDeclined(userId, "Dietologist", "declined-id");
        Notification dueSoon = NotificationFactory.CreateClientTaskDueSoon(userId);

        Assert.Multiple(
            () => Assert.Equal(NotificationTypes.DietologistInvitationReceived, received.Type),
            () => Assert.Equal("received-id", received.ReferenceId),
            () => Assert.Equal(NotificationTypes.DietologistInvitationAccepted, accepted.Type),
            () => Assert.Equal("accepted-id", accepted.ReferenceId),
            () => Assert.Equal(NotificationTypes.DietologistInvitationDeclined, declined.Type),
            () => Assert.Equal("declined-id", declined.ReferenceId),
            () => Assert.Equal(NotificationTypes.ClientTaskDueSoon, dueSoon.Type),
            () => Assert.Null(dueSoon.ReferenceId));
    }
}
