using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class ApiWebApplicationFactory : PostgresApiWebApplicationFactory {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => {
            var applicationPartManager = services
                .Single(service => service.ServiceType == typeof(ApplicationPartManager))
                .ImplementationInstance as ApplicationPartManager;
            applicationPartManager?.ApplicationParts.Add(new AssemblyPart(typeof(TestExceptionController).Assembly));
        });
    }
}
