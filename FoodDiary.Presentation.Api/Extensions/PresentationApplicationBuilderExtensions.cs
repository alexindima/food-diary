using FoodDiary.Presentation.Api.Hubs;
using Microsoft.AspNetCore.Builder;

namespace FoodDiary.Presentation.Api.Extensions;

public static class PresentationApplicationBuilderExtensions {
    extension(WebApplication app) {
        public WebApplication MapPresentationApi(string corsPolicyName) {
            app.MapControllers();
            app.MapHub<EmailVerificationHub>("/hubs/email-verification")
                .RequireCors(corsPolicyName);
            app.MapHub<NotificationHub>("/hubs/notifications")
                .RequireCors(corsPolicyName);

            return app;
        }
    }
}
