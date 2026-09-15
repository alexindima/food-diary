using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Modules.Identity.Presentation.Services;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Identity.Presentation.Filters;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Identity.Presentation.Extensions;

public static class IdentityPresentationServiceCollectionExtensions {
    public static IServiceCollection AddIdentityPresentation(this IServiceCollection services) {
        services.AddScoped<AuthenticationCookieResultFilter>();
        services.AddScoped<RefreshTokenCookieService>();
        services.AddScoped<TelegramBrowserBindingHttpProcessor>();
        services.AddScoped<TelegramBotSecretAuthorizationFilter>();
        services.AddScoped<IEmailVerificationNotifier, EmailVerificationNotifier>();
        services.Configure<MvcOptions>(options => options.Filters.AddService<AuthenticationCookieResultFilter>());
        return services.AddPresentationAssembly(typeof(IdentityPresentationServiceCollectionExtensions).Assembly);
    }
}
