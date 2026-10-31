namespace FoodDiary.Modules.Notifications.Presentation.Requests;

public sealed record NotificationReadStatusHttpRequest(IReadOnlyList<Guid> NotificationIds);
