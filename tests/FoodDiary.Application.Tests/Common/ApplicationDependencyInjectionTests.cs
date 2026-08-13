using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Services;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Dashboard;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Application.Notifications;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Tests.Common;

[ExcludeFromCodeCoverage]
public sealed class ApplicationDependencyInjectionTests {
    [Fact]
    public void AddApplication_RegistersCoreApplicationServices() {
        var services = new ServiceCollection();

        DependencyInjection.AddApplication(services);

        Assert.Contains(services, ServiceDescriptorMatches<IPostCommitActionQueue, PostCommitActionQueue>(ServiceLifetime.Scoped));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(INotificationCleanupService));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(TimeProvider) &&
            descriptor.Lifetime == ServiceLifetime.Singleton &&
            ReferenceEquals(descriptor.ImplementationInstance, TimeProvider.System));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IProductSearchSuggestionProvider));
        Assert.Contains(services, d => d.ServiceType.IsGenericType && string.Equals(d.ServiceType.GetGenericTypeDefinition().FullName, "FluentValidation.IValidator`1", StringComparison.Ordinal));
        Assert.Contains(services, d => d.ImplementationType == typeof(LoggingBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType == typeof(ValidationBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType == typeof(CommandTransactionBehavior<,>));
    }

    [Fact]
    public void AddProductsModule_RegistersProductServices() {
        var services = new ServiceCollection();

        services.AddProductsModule();

        Assert.Equal(2, services.Count(descriptor => descriptor.ServiceType == typeof(IProductSearchSuggestionProvider)));
    }

    [Fact]
    public void AddDashboardModule_RegistersDashboardServices() {
        var services = new ServiceCollection();

        services.AddDashboardModule();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IDashboardSnapshotBuilder) &&
            descriptor.ImplementationFactory is not null &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddNotificationsModule_RegistersNotificationServices() {
        var services = new ServiceCollection();

        services.AddNotificationsModule();

        Assert.Contains(services, ServiceDescriptorMatches<INotificationCleanupService, NotificationCleanupService>(ServiceLifetime.Scoped));
    }

    private static Predicate<ServiceDescriptor> ServiceDescriptorMatches<TService, TImplementation>(ServiceLifetime lifetime) =>
        descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == lifetime;
}
