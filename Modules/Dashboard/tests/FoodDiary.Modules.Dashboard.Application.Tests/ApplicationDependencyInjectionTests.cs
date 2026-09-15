using FoodDiary.Modules.Dashboard.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Dashboard.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationDependencyInjectionTests {
    [Fact]
    public void AddDashboardModule_RegistersDashboardServices() {
        var services = new ServiceCollection();

        services.AddDashboardModule();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IDashboardSnapshotBuilder) &&
            descriptor.ImplementationFactory is not null &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
