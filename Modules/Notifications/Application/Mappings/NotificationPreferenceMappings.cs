using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Mappings;

internal static class NotificationPreferenceMappings {
    internal static NotificationPreferencesModel ToModel(UserNotificationProfileModel user) =>
        new(
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours);

}
