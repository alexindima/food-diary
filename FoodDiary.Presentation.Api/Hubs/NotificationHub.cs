using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Presentation.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub {
}

public static class NotificationHubMethods {
    public const string UnreadCountUpdated = "UnreadCountUpdated";
    public const string NotificationsChanged = "NotificationsChanged";
}
