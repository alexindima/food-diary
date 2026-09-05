using FoodDiary.Presentation.Api.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;

namespace FoodDiary.Presentation.Api.Extensions;

public static class NotificationsPresentationApplicationBuilderExtensions {
    public static WebApplication MapNotificationsPresentationHub(
        this WebApplication app,
        string corsPolicyName) {
        app.MapHub<NotificationHub>("/hubs/notifications", ConfigureAuthenticationLifetime)
            .RequireCors(corsPolicyName);
        return app;
    }

    private static void ConfigureAuthenticationLifetime(HttpConnectionDispatcherOptions options) =>
        options.CloseOnAuthenticationExpiration = true;
}
