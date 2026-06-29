using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
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

[ExcludeFromCodeCoverage]
public sealed class ApplicationDependencyInjectionTests {
    [Fact]
    public void AddApplication_RegistersCoreApplicationServices() {
        var services = new ServiceCollection();

        DependencyInjection.AddApplication(services);

        Assert.Contains(services, ServiceDescriptorMatches<IMealNutritionService, MealNutritionService>(ServiceLifetime.Scoped));
        Assert.Contains(services, ServiceDescriptorMatches<IDashboardSnapshotBuilder, DashboardSnapshotBuilder>(ServiceLifetime.Scoped));
        Assert.Contains(services, ServiceDescriptorMatches<IPostCommitActionQueue, PostCommitActionQueue>(ServiceLifetime.Scoped));
        Assert.Contains(services, ServiceDescriptorMatches<INotificationCleanupService, NotificationCleanupService>(ServiceLifetime.Scoped));
        Assert.Contains(services, ServiceDescriptorMatches<IAuthenticationTokenService, AuthenticationTokenService>(ServiceLifetime.Scoped));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(TimeProvider) &&
            descriptor.Lifetime == ServiceLifetime.Singleton &&
            ReferenceEquals(descriptor.ImplementationInstance, TimeProvider.System));
        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(IProductSearchSuggestionProvider)));
        Assert.Contains(services, d => d.ServiceType.IsGenericType && string.Equals(d.ServiceType.GetGenericTypeDefinition().FullName, "FluentValidation.IValidator`1", StringComparison.Ordinal));
        Assert.Contains(services, d => d.ImplementationType == typeof(LoggingBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType == typeof(ValidationBehavior<,>));
        Assert.Contains(services, d => d.ImplementationType == typeof(PostCommitBehavior<,>));
        Assert.Contains(services, d => string.Equals(d.ImplementationType?.Name, "UnitOfWorkBehavior`2", StringComparison.Ordinal));
        AssertBehaviorRegisteredBefore(services, typeof(PostCommitBehavior<,>), typeof(UnitOfWorkBehavior<,>));
    }

    private static Predicate<ServiceDescriptor> ServiceDescriptorMatches<TService, TImplementation>(ServiceLifetime lifetime) =>
        descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == lifetime;

    private static void AssertBehaviorRegisteredBefore(IServiceCollection services, Type beforeType, Type afterType) {
        List<ServiceDescriptor> behaviors = [.. services.Where(descriptor =>
            descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(FoodDiary.Mediator.IPipelineBehavior<,>))];
        int beforeIndex = behaviors.FindIndex(descriptor => descriptor.ImplementationType == beforeType);
        int afterIndex = behaviors.FindIndex(descriptor => descriptor.ImplementationType == afterType);

        Assert.True(beforeIndex >= 0, $"{beforeType.Name} is not registered.");
        Assert.True(afterIndex >= 0, $"{afterType.Name} is not registered.");
        Assert.True(beforeIndex < afterIndex, $"{beforeType.Name} must be registered before {afterType.Name}.");
    }
}
