using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Presentation.Api.Services;

namespace FoodDiary.Presentation.Api.Extensions;

public static class NotificationsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddNotificationsPresentation(this IServiceCollection services) {
        services.AddScoped<INotificationPusher, NotificationPusher>();
        return services.AddPresentationAssembly(typeof(NotificationsPresentationServiceCollectionExtensions).Assembly);
    }
}
