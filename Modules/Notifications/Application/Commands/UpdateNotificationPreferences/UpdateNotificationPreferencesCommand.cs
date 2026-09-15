using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Commands.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    Guid? UserId,
    bool? PushNotificationsEnabled,
    bool? FastingPushNotificationsEnabled,
    bool? SocialPushNotificationsEnabled,
    int? FastingCheckInReminderHours,
    int? FastingCheckInFollowUpReminderHours)
    : ICommand<Result<NotificationPreferencesModel>>, IUserRequest;
