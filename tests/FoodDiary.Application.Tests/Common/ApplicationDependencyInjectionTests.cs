using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Services;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Application.Products.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Tests.Common;

public sealed class ApplicationDependencyInjectionTests {
    [Fact]
    public void AddApplication_RegistersCoreApplicationServices() {
        var services = new ServiceCollection();

        DependencyInjection.AddApplication(services);

        Assert.Contains(services, ServiceDescriptorMatches<IMealNutritionService, MealNutritionService>(ServiceLifetime.Scoped));
        Assert.Contains(services, ServiceDescriptorMatches<IDashboardSnapshotBuilder, DashboardSnapshotBuilder>(ServiceLifetime.Scoped));
        Assert.Contains(services, ServiceDescriptorMatches<INotificationCleanupService, NotificationCleanupService>(ServiceLifetime.Scoped));
        Assert.Contains(services, ServiceDescriptorMatches<IAuthenticationTokenService, AuthenticationTokenService>(ServiceLifetime.Scoped));
        Assert.Contains(services, ServiceDescriptorMatches<IDateTimeProvider, SystemDateTimeProvider>(ServiceLifetime.Singleton));
        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(IProductSearchSuggestionProvider)));
        Assert.Contains(services, d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition().FullName == "FluentValidation.IValidator`1");
        Assert.Contains(services, d => d.ImplementationType == typeof(LoggingBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType == typeof(ValidationBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType?.Name == "UnitOfWorkBehavior`2");
    }

    private static Predicate<ServiceDescriptor> ServiceDescriptorMatches<TService, TImplementation>(ServiceLifetime lifetime) =>
        descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == lifetime;
}
