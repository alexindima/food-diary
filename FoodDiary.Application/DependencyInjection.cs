using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Images.Services;
using FluentValidation;
using System.Reflection;

namespace FoodDiary.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR: register handlers from this assembly.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Add request validation into MediatR pipeline.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation: register validators from this assembly.
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IImageAssetCleanupService, ImageAssetCleanupService>();

        return services;
    }
}
