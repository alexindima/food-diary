using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Services;
using FoodDiary.Application.Images.Services;
using FluentValidation;
using System.Reflection;

namespace FoodDiary.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IImageAssetCleanupService, ImageAssetCleanupService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();

        return services;
    }
}
