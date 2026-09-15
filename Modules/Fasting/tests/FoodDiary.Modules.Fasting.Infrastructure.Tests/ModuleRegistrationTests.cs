using FoodDiary.Modules.Fasting.Application.Abstractions.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Fasting.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddFastingModule_RegistersAggregateRepositoriesAndTheirPorts() {
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddFastingModule();

        Assert.Same(services, returned);
        Type[] requiredPorts = [
            typeof(IFastingPlanRepository), typeof(IFastingPlanReadRepository), typeof(IFastingPlanWriteRepository),
            typeof(IFastingOccurrenceRepository), typeof(IFastingOccurrenceReadRepository),
            typeof(IFastingOccurrenceReadModelRepository), typeof(IFastingOccurrenceWriteRepository),
            typeof(IFastingCheckInRepository), typeof(IFastingCheckInReadRepository),
            typeof(IFastingCheckInReadModelRepository), typeof(IFastingCheckInWriteRepository),
            typeof(IFastingSessionRepository), typeof(IFastingSessionReadRepository), typeof(IFastingSessionWriteRepository),
            typeof(IFastingTelemetryEventRepository), typeof(IFastingTelemetryEventReadRepository),
            typeof(IFastingTelemetryEventWriteRepository),
        ];
        Assert.All(requiredPorts, port => Assert.Contains(services, descriptor => descriptor.ServiceType == port));
    }
}
