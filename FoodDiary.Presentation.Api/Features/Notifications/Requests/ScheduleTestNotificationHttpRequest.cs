using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Notifications.Common;

namespace FoodDiary.Presentation.Api.Features.Notifications.Requests;

public sealed record ScheduleTestNotificationHttpRequest(
    [param: Range(1, 3600)] int DelaySeconds = 10,
    string Type = NotificationTypes.FastingCompleted);
