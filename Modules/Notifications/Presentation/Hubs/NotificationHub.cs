using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Modules.Notifications.Presentation.Hubs;

[Authorize]
public sealed class NotificationHub : Hub;
