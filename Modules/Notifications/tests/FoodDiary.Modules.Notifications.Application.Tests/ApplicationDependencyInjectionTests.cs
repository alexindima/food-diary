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
        FoodDiary.Modules.Notifications.Application.Abstractions.Common.INotificationLookupRepository lookup = NSubstitute.Substitute.For<FoodDiary.Modules.Notifications.Application.Abstractions.Common.INotificationLookupRepository>();
        IServiceProvider provider = NSubstitute.Substitute.For<IServiceProvider>();
        provider.GetService(typeof(FoodDiary.Modules.Notifications.Application.Abstractions.Common.INotificationLookupRepository)).Returns(lookup);
        ServiceDescriptor alias = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(FoodDiary.Modules.Notifications.Contracts.Common.INotificationDeduplicationService));
        Assert.Same(lookup, alias.ImplementationFactory!(provider));

        Assert.Contains(services, ServiceDescriptorMatches<IRequestHandler<CleanupExpiredNotificationsCommand, int>, CleanupExpiredNotificationsCommandHandler>(ServiceLifetime.Transient));
    }

    private static Predicate<ServiceDescriptor> ServiceDescriptorMatches<TService, TImplementation>(ServiceLifetime lifetime) =>
        descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == lifetime;
}
