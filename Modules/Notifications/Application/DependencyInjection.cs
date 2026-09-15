using FluentValidation;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Notifications.Application;

public static class DependencyInjection {
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<INotificationClientRefreshService, NotificationClientRefreshService>();
        services.AddScoped<INotificationDeduplicationService>(serviceProvider =>
            serviceProvider.GetRequiredService<INotificationLookupRepository>());
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
