using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Resources.Notifications;

namespace FoodDiary.JobManager.Services;

public static class NotificationResourceServiceCollectionExtensions {
    public static IServiceCollection AddNotificationResources(this IServiceCollection services) {
        services.AddSingleton<INotificationTextRenderer, NotificationResourceRenderer>();

        return services;
    }
}
