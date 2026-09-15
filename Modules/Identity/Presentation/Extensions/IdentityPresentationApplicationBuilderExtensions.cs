using FoodDiary.Modules.Identity.Presentation.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;

namespace FoodDiary.Modules.Identity.Presentation.Extensions;

public static class IdentityPresentationApplicationBuilderExtensions {
    public static WebApplication MapIdentityPresentationHub(
        this WebApplication app,
        string corsPolicyName) {
        app.MapHub<EmailVerificationHub>("/hubs/email-verification", ConfigureAuthenticationLifetime)
            .RequireCors(corsPolicyName);
        return app;
    }

    private static void ConfigureAuthenticationLifetime(HttpConnectionDispatcherOptions options) =>
        options.CloseOnAuthenticationExpiration = true;
}
