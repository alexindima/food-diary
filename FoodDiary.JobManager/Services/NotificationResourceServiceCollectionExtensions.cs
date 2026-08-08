using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Resources.Notifications;

namespace FoodDiary.JobManager.Services;

public static class NotificationResourceServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public void AddNotificationResources() {
            services.AddSingleton<INotificationTextRenderer, NotificationResourceRenderer>();
        }
    }
}
