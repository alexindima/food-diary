using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Modules.Notifications.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;

namespace FoodDiary.Modules.Notifications.Presentation.Extensions;

public static class NotificationsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddNotificationsPresentation(this IServiceCollection services) {
        services.AddScoped<INotificationPusher, NotificationPusher>();
        return services.AddPresentationAssembly(typeof(NotificationsPresentationServiceCollectionExtensions).Assembly);
    }
}
