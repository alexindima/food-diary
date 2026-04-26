using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    Guid? UserId,
    bool? PushNotificationsEnabled,
    bool? FastingPushNotificationsEnabled,
    bool? SocialPushNotificationsEnabled,
    int? FastingCheckInReminderHours,
    int? FastingCheckInFollowUpReminderHours)
    : ICommand<Result<NotificationPreferencesModel>>, IUserRequest;
