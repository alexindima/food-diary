using FoodDiary.Application.Abstractions.DailyAdvices.Common;
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
        Assert.Equal(typeof(DailyAdviceRepository), descriptor.ImplementationType);
    }
}
