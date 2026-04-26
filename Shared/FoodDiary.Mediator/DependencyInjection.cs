using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator;

public sealed class MediatorServiceConfiguration {
    internal List<Assembly> Assemblies { get; } = [];

    internal List<Type> OpenBehaviors { get; } = [];

    public void RegisterServicesFromAssembly(Assembly assembly) {
        Assemblies.Add(assembly);
    }

    public void AddOpenBehavior(Type behaviorType) {
        if (!behaviorType.IsGenericTypeDefinition) {
            throw new ArgumentException("Behavior type must be an open generic type definition.", nameof(behaviorType));
        }

        OpenBehaviors.Add(behaviorType);
    }
}

public static class DependencyInjection {
    public static IServiceCollection AddFoodDiaryMediator(
        this IServiceCollection services,
        Action<MediatorServiceConfiguration> configure) {
        var configuration = new MediatorServiceConfiguration();
        configure(configuration);

        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(static provider => provider.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(static provider => provider.GetRequiredService<IMediator>());

        foreach (var assembly in configuration.Assemblies.Distinct()) {
            services.RegisterMediatorHandlers(assembly);
        }

        foreach (var behaviorType in configuration.OpenBehaviors) {
            services.AddTransient(typeof(IPipelineBehavior<,>), behaviorType);
        }

        return services;
    }

    private static void RegisterMediatorHandlers(this IServiceCollection services, Assembly assembly) {
        var implementationTypes = assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false })
            .ToArray();

        foreach (var implementationType in implementationTypes) {
            foreach (var serviceType in implementationType.GetInterfaces().Where(IsMediatorHandler)) {
                services.AddTransient(serviceType, implementationType);
            }
        }
    }

    private static bool IsMediatorHandler(Type interfaceType) {
        if (!interfaceType.IsGenericType) {
            return false;
        }

        var genericDefinition = interfaceType.GetGenericTypeDefinition();
        return genericDefinition == typeof(IRequestHandler<,>) ||
            genericDefinition == typeof(INotificationHandler<>);
    }
}
