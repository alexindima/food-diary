using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Modules.Notifications.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure.Persistence.Notifications;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using FoodDiary.Modules.Notifications.Infrastructure.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Notifications.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddNotificationResources(this IServiceCollection services) =>
        services.AddSingleton<INotificationTextRenderer, NotificationResourceRenderer>();

    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.AddNotificationsPersistence();
        return services.AddNotificationsProvider(configuration);
    }

    public static IServiceCollection AddNotificationsPersistence(this IServiceCollection services) {
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<NotificationsDbContext>(static options => new NotificationsDbContext(options)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxReplayStream, WebPushOutboxReplayStream>());
        services.AddScoped<INotificationRepository>(static provider =>
            new NotificationRepository(
                provider.GetRequiredService<NotificationsDbContext>().Notifications,
                provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<INotificationReadRepository>(static provider => provider.GetRequiredService<INotificationRepository>());
        services.AddScoped<INotificationLookupRepository>(static provider => provider.GetRequiredService<INotificationRepository>());
        services.AddScoped<INotificationReadModelRepository>(static provider => provider.GetRequiredService<INotificationRepository>());
        services.AddScoped<INotificationWriteRepository>(static provider => provider.GetRequiredService<INotificationRepository>());
        services.AddScoped<INotificationWebPushOutbox>(static provider => new NotificationWebPushOutbox(
            provider.GetRequiredService<NotificationsDbContext>().NotificationWebPushOutbox, provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<INotificationWebPushOutboxProcessor>(static provider => {
            NotificationsDbContext owned = provider.GetRequiredService<NotificationsDbContext>();
            FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
            return new NotificationWebPushOutboxProcessor(owned, owned.NotificationWebPushOutbox,
                provider.GetRequiredService<IWebPushNotificationSender>(), provider.GetRequiredService<IOptions<OutboxProcessingOptions>>(),
                provider.GetRequiredService<TimeProvider>(), provider.GetRequiredService<ILogger<NotificationWebPushOutboxProcessor>>(),
                () => OutboxProcessingEngine.EnsureCleanEntry(shared));
        });
        services.AddScoped<IWebPushSubscriptionRepository>(static provider =>
            new WebPushSubscriptionRepository(provider.GetRequiredService<NotificationsDbContext>().WebPushSubscriptions));
        services.AddScoped<IWebPushSubscriptionReadRepository>(static provider => provider.GetRequiredService<IWebPushSubscriptionRepository>());
        services.AddScoped<IWebPushSubscriptionReadModelRepository>(static provider => provider.GetRequiredService<IWebPushSubscriptionRepository>());
        services.AddScoped<IWebPushSubscriptionWriteRepository>(static provider => provider.GetRequiredService<IWebPushSubscriptionRepository>());
        return services;
    }

    public static IServiceCollection AddNotificationsProvider(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<WebPushOptions>()
            .Bind(configuration.GetSection(WebPushOptions.SectionName))
            .Validate(WebPushOptions.HasValidConfiguration, "WebPush configuration is invalid.")
            .ValidateOnStart();
        services.AddTransient<WebPushEndpointValidationHandler>();
        services.AddHttpClient<IWebPushClientAdapter, WebPushClientAdapter>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(WebPushSocketsHttpHandlerFactory.Create)
            .AddHttpMessageHandler<WebPushEndpointValidationHandler>();
        services.AddScoped<IWebPushNotificationSender, WebPushNotificationSender>();
        services.AddScoped<IWebPushConfigurationProvider, WebPushNotificationSender>();
        return services;
    }
}
