using FoodDiary.Modules.Recipes.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Recipes.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationDependencyInjectionTests {
    [Fact]
    public void AddRecipesApplication_RegistersRecipeServices() {
        var services = new ServiceCollection();

        services.AddRecipesApplication();

        Assert.Contains(services, ServiceDescriptorMatches<RecentRecipeLoader, RecentRecipeLoader>(ServiceLifetime.Scoped));
    }

    private static Predicate<ServiceDescriptor> ServiceDescriptorMatches<TService, TImplementation>(ServiceLifetime lifetime) =>
        descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == lifetime;
}
