using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class TestAuthApiWebApplicationFactory : PostgresApiWebApplicationFactory {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSyntheticAuthentication());
    }
}
