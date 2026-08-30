using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Modules.Fasting.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

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
