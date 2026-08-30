namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationTextRenderer {
    NotificationText Render(string type, string? locale = null, params object[] arguments);
    NotificationText RenderFromPayload(string type, string payloadJson, string? locale = null);
}
