using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Mediator;

/// <summary>
/// Registers the FoodDiary mediator and its handlers with dependency injection.
/// </summary>
public static class DependencyInjection {
    /// <summary>
    /// Adds the mediator services, discovered handlers, and configured pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">The mediator configuration callback.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddFoodDiaryMediator(
        this IServiceCollection services,
        Action<MediatorServiceConfiguration> configure) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new MediatorServiceConfiguration();
        configure(configuration);

        services.TryAddScoped<IMediator, DefaultMediator>();
        services.TryAddScoped<ISender>(static provider => provider.GetRequiredService<IMediator>());
        services.TryAddScoped<IPublisher>(static provider => provider.GetRequiredService<IMediator>());

        foreach (Assembly assembly in configuration.Assemblies) {
            services.RegisterMediatorHandlers(assembly);
        }

        foreach (Type behaviorType in configuration.OpenBehaviors) {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), behaviorType));
        }

        ValidateSingleHandlerRegistrations(services);

        return services;
    }

    private static void RegisterMediatorHandlers(this IServiceCollection services, Assembly assembly) {
        Type[] implementationTypes = [.. assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false })
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)];

        foreach (Type implementationType in implementationTypes) {
            foreach (Type serviceType in implementationType
                .GetInterfaces()
                .Where(IsMediatorHandler)
                .OrderBy(static type => type.FullName, StringComparer.Ordinal)) {
                services.TryAddEnumerable(ServiceDescriptor.Transient(serviceType, implementationType));
            }
        }
    }

    private static void ValidateSingleHandlerRegistrations(IServiceCollection services) {
        IGrouping<Type, ServiceDescriptor>? duplicateRegistration = services
            .Where(static descriptor => !descriptor.IsKeyedService)
            .Where(static descriptor => IsSingleHandler(descriptor.ServiceType))
            .GroupBy(static descriptor => descriptor.ServiceType)
            .FirstOrDefault(static group => group.Skip(1).Any());

        if (duplicateRegistration is null) {
            return;
        }

        throw new InvalidOperationException(
            $"Mediator handler contract {duplicateRegistration.Key} has multiple registrations.");
    }

    private static bool IsMediatorHandler(Type interfaceType) {
        if (!interfaceType.IsGenericType) {
            return false;
        }

        Type genericDefinition = interfaceType.GetGenericTypeDefinition();
        return genericDefinition == typeof(IRequestHandler<,>) ||
            genericDefinition == typeof(INotificationHandler<>) ||
            genericDefinition == typeof(IStreamRequestHandler<,>);
    }

    private static bool IsSingleHandler(Type serviceType) {
        if (!serviceType.IsGenericType) {
            return false;
        }

        Type genericDefinition = serviceType.GetGenericTypeDefinition();
        return genericDefinition == typeof(IRequestHandler<,>) ||
            genericDefinition == typeof(IStreamRequestHandler<,>);
    }
}
