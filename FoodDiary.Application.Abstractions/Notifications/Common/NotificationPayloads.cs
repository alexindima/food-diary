namespace FoodDiary.Application.Notifications.Common;

public static class NotificationPayloads {
    public static string Empty() => NotificationPayloadSerializer.Serialize(new EmptyNotificationPayload());

    public static string NewRecommendation(string dietologistName) =>
        NotificationPayloadSerializer.Serialize(new NewRecommendationNotificationPayload(dietologistName));
}

public sealed record EmptyNotificationPayload;

public sealed record NewRecommendationNotificationPayload(string DietologistName);
