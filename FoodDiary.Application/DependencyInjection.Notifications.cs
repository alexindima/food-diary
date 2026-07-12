using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Images.Services;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Application.Users.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddNotificationModule(this IServiceCollection services) {
        services.AddScoped<IImageAssetAccessService, ImageAssetAccessService>();
        services.AddScoped<IImageAssetCleanupService, ImageAssetCleanupService>();
        services.AddScoped<INotificationCleanupService, NotificationCleanupService>();
        services.AddScoped<INotificationClientRefreshService, NotificationClientRefreshService>();
        services.AddScoped<INotificationDeduplicationService>(serviceProvider =>
            serviceProvider.GetRequiredService<INotificationLookupRepository>());
        services.AddScoped<INotificationUserAccessService, NotificationUserAccessService>();
        services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
        services.AddScoped<INotificationFeedReadService, NotificationFeedReadService>();
        services.AddScoped<IWebPushSubscriptionReadService, WebPushSubscriptionReadService>();
        services.AddScoped<IProfileNotificationReadService>(static provider =>
            (IProfileNotificationReadService)provider.GetRequiredService<IWebPushSubscriptionReadService>());
        services.AddScoped<IWebPushDeliveryAudienceService, WebPushDeliveryAudienceService>();
        services.AddScoped<INotificationUserContextService, NotificationUserContextService>();
        services.AddScoped<INotificationWriter, NotificationWriter>();
        services.AddScoped<IOpenAiFoodService, OpenAiFoodService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IDietologistEmailSender, DietologistEmailSender>();
    }
}
