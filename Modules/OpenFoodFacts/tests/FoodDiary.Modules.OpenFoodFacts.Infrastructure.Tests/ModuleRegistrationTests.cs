using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddOpenFoodFactsModule_RegistersCacheRepositoryPorts() {
        var services = new ServiceCollection();

        services.AddOpenFoodFactsModule();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IOpenFoodFactsProductCacheRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IOpenFoodFactsProductCacheReadRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IOpenFoodFactsProductCacheWriteRepository));
    }
}
