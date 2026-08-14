using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Notifications;

[ExcludeFromCodeCoverage]
public sealed class NotificationFactoryTests {
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
}
