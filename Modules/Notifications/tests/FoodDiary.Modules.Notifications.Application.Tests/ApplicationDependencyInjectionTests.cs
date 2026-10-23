using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Mediator;
using FoodDiary.Modules.Notifications.Application.Commands.CleanupExpiredNotifications;
using FoodDiary.Modules.Notifications.Contracts.Commands.CleanupExpiredNotifications;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationDependencyInjectionTests {
    [Fact]
    public void AddNotificationsModule_RegistersNotificationServices() {
        var services = new ServiceCollection();

        services.AddNotificationsModule();
        INotificationLookupRepository lookup = Substitute.For<INotificationLookupRepository>();
        IServiceProvider provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(INotificationLookupRepository)).Returns(lookup);
        ServiceDescriptor alias = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(INotificationDeduplicationService));
        Assert.Same(lookup, alias.ImplementationFactory!(provider));

        Assert.Contains(services, ServiceDescriptorMatches<IRequestHandler<CleanupExpiredNotificationsCommand, int>, CleanupExpiredNotificationsCommandHandler>(ServiceLifetime.Transient));
    }

    private static Predicate<ServiceDescriptor> ServiceDescriptorMatches<TService, TImplementation>(ServiceLifetime lifetime) =>
        descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == lifetime;
}
