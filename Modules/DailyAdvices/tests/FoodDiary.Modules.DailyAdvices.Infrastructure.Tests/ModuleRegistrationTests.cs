using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddDailyAdvicesModule_RegistersOwnedRepository() {
        var services = new ServiceCollection();
        services.AddDailyAdvicesModule();
        ServiceDescriptor descriptor = Assert.Single(services, item => item.ServiceType == typeof(IDailyAdviceReadModelRepository));
        Assert.NotNull(descriptor.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Contains(services, item => item.ServiceType == typeof(DailyAdvicesDbContext)
            && item.Lifetime == ServiceLifetime.Scoped);
    }
    [Fact]
    public void RegistersSeparateScopedWriter() {
        var services = new ServiceCollection();
        services.AddDailyAdvicesModule();
        ServiceDescriptor descriptor = Assert.Single(services, item => item.ServiceType == typeof(IDailyAdviceWriteRepository));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

}
