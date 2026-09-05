using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Presentation.Api.Filters;

namespace FoodDiary.Presentation.Api.Extensions;

public static class IdentityPresentationServiceCollectionExtensions {
    public static IServiceCollection AddIdentityPresentation(this IServiceCollection services) {
        services.AddScoped<AuthenticationCookieResultFilter>();
        services.Configure<MvcOptions>(options => options.Filters.AddService<AuthenticationCookieResultFilter>());
        return services.AddPresentationAssembly(typeof(IdentityPresentationServiceCollectionExtensions).Assembly);
    }
}
