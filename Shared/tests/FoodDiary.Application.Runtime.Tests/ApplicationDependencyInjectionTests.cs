using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Runtime.Common.Services;
using FoodDiary.Application.Runtime.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Runtime.Tests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationDependencyInjectionTests {
    [Fact]
    public void AddApplicationRuntime_RegistersCoreApplicationServices() {
        var services = new ServiceCollection();

        FoodDiary.Application.Runtime.DependencyInjection.AddApplicationRuntime(services);

        Assert.Contains(services, ServiceDescriptorMatches<IPostCommitActionQueue, PostCommitActionQueue>(ServiceLifetime.Scoped));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(TimeProvider) &&
            descriptor.Lifetime == ServiceLifetime.Singleton &&
            ReferenceEquals(descriptor.ImplementationInstance, TimeProvider.System));
        Assert.DoesNotContain(services, d => d.ServiceType.IsGenericType && string.Equals(d.ServiceType.GetGenericTypeDefinition().FullName, "FluentValidation.IValidator`1", StringComparison.Ordinal));
        Assert.Contains(services, d => d.ImplementationType == typeof(LoggingBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType == typeof(ModuleTelemetryBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType == typeof(ValidationBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType == typeof(CommandTransactionBehavior<,>));
    }

    private static Predicate<ServiceDescriptor> ServiceDescriptorMatches<TService, TImplementation>(ServiceLifetime lifetime) =>
        descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == lifetime;
}
