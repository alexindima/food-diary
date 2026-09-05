using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Presentation.Api.Services;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Extensions;

public static class IdentityPresentationServiceCollectionExtensions {
    public static IServiceCollection AddIdentityPresentation(this IServiceCollection services) {
        services.AddScoped<AuthenticationCookieResultFilter>();
        services.AddScoped<RefreshTokenCookieService>();
        services.AddScoped<TelegramBotSecretAuthorizationFilter>();
        services.AddScoped<IEmailVerificationNotifier, EmailVerificationNotifier>();
        services.Configure<MvcOptions>(options => options.Filters.AddService<AuthenticationCookieResultFilter>());
        return services.AddPresentationAssembly(typeof(IdentityPresentationServiceCollectionExtensions).Assembly);
    }
}
