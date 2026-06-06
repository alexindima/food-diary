using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator;

public static class DependencyInjection {
    public static IServiceCollection AddFoodDiaryMediator(
        this IServiceCollection services,
        Action<MediatorServiceConfiguration> configure) {
        var configuration = new MediatorServiceConfiguration();
        configure(configuration);

        services.AddScoped<IMediator, DefaultMediator>();
        services.AddScoped<ISender>(static provider => provider.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(static provider => provider.GetRequiredService<IMediator>());

        foreach (Assembly assembly in configuration.Assemblies.Distinct()) {
            services.RegisterMediatorHandlers(assembly);
        }

        foreach (Type behaviorType in configuration.OpenBehaviors) {
            services.AddTransient(typeof(IPipelineBehavior<,>), behaviorType);
        }

        return services;
    }

    private static void RegisterMediatorHandlers(this IServiceCollection services, Assembly assembly) {
        Type[] implementationTypes = [.. assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false })];

        foreach (Type implementationType in implementationTypes) {
            foreach (Type serviceType in implementationType.GetInterfaces().Where(IsMediatorHandler)) {
                services.AddTransient(serviceType, implementationType);
            }
        }
    }

    private static bool IsMediatorHandler(Type interfaceType) {
        if (!interfaceType.IsGenericType) {
            return false;
        }

        Type genericDefinition = interfaceType.GetGenericTypeDefinition();
        return genericDefinition == typeof(IRequestHandler<,>) ||
            genericDefinition == typeof(INotificationHandler<>);
    }
}
