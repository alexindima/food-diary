using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Cycles.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class CyclesModuleRegistrationTests {
    [Fact]
    public void AddCyclesModule_RegistersRepositoryAndAllNarrowAliases() {
        var services = new ServiceCollection();
        services.AddCyclesModule();

        Assert.Multiple(
            () => Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ICycleRepository) && descriptor.ImplementationType == typeof(CycleRepository)),
            () => Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ICycleReadRepository)),
            () => Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ICycleReadModelRepository)),
            () => Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ICycleWriteRepository)));
    }
}
