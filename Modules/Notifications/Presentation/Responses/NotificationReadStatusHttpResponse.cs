namespace FoodDiary.Modules.Notifications.Presentation.Responses;

public sealed record NotificationReadStatusHttpResponse(IReadOnlyList<Guid> ReadIds);
