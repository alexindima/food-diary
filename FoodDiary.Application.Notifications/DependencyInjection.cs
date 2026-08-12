using FluentValidation;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Notifications;

public static class DependencyInjection {
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<INotificationCleanupService, NotificationCleanupService>();
        services.AddScoped<INotificationClientRefreshService, NotificationClientRefreshService>();
        services.AddScoped<INotificationDeduplicationService>(serviceProvider =>
            serviceProvider.GetRequiredService<INotificationLookupRepository>());
        services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
        services.AddScoped<INotificationFeedReadService, NotificationFeedReadService>();
        services.AddScoped<IWebPushSubscriptionReadService, WebPushSubscriptionReadService>();
        services.AddScoped<IProfileNotificationReadService>(static provider =>
            (IProfileNotificationReadService)provider.GetRequiredService<IWebPushSubscriptionReadService>());
        services.AddScoped<IWebPushDeliveryAudienceService, WebPushDeliveryAudienceService>();
        services.AddScoped<INotificationUserContextService, NotificationUserContextService>();
        services.AddScoped<INotificationWriter, NotificationWriter>();
        services.AddScoped<ITestNotificationDeliveryDispatcher, TestNotificationDeliveryDispatcher>();

        return services;
    }
}
