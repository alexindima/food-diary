using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddNotificationPersistence(this IServiceCollection services) {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationReadRepository>(static provider => provider.GetRequiredService<INotificationRepository>());
        services.AddScoped<INotificationReadModelRepository>(static provider => provider.GetRequiredService<INotificationRepository>());
        services.AddScoped<INotificationWriteRepository>(static provider => provider.GetRequiredService<INotificationRepository>());
        services.AddScoped<IEmailOutbox, EmailOutbox>();
        services.AddScoped<IEmailOutboxProcessor, EmailOutboxProcessor>();
        services.AddScoped<INotificationWebPushOutbox, NotificationWebPushOutbox>();
        services.AddScoped<INotificationWebPushOutboxProcessor, NotificationWebPushOutboxProcessor>();
        services.AddScoped<IWebPushSubscriptionRepository, WebPushSubscriptionRepository>();
        services.AddScoped<IWebPushSubscriptionReadRepository>(static provider => provider.GetRequiredService<IWebPushSubscriptionRepository>());
        services.AddScoped<IWebPushSubscriptionReadModelRepository>(static provider => provider.GetRequiredService<IWebPushSubscriptionRepository>());
        services.AddScoped<IWebPushSubscriptionWriteRepository>(static provider => provider.GetRequiredService<IWebPushSubscriptionRepository>());

        return services;
    }
}
