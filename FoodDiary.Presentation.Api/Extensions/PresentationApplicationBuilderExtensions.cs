using FoodDiary.Presentation.Api.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;

namespace FoodDiary.Presentation.Api.Extensions;

public static class PresentationApplicationBuilderExtensions {
    extension(WebApplication app) {
        public WebApplication MapPresentationApi(string corsPolicyName) {
            app.MapControllers();
            app.MapHub<EmailVerificationHub>("/hubs/email-verification", ConfigureHubAuthenticationLifetime)
                .RequireCors(corsPolicyName);
            app.MapHub<NotificationHub>("/hubs/notifications", ConfigureHubAuthenticationLifetime)
                .RequireCors(corsPolicyName);

            return app;
        }
    }

    private static void ConfigureHubAuthenticationLifetime(HttpConnectionDispatcherOptions options) =>
        options.CloseOnAuthenticationExpiration = true;
}
