using System.ComponentModel.DataAnnotations;
using FoodDiary.Modules.Notifications.Contracts.Common;

namespace FoodDiary.Modules.Notifications.Presentation.Requests;

public sealed record ScheduleTestNotificationHttpRequest(
    [param: Range(1, 3600)] int DelaySeconds = 10,
    string Type = NotificationTypes.FastingCompleted);
