using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

// For Swagger, authentication and pre-handler transport checks. Workers are disabled
// so these checks perform no database or provider I/O.
[ExcludeFromCodeCoverage]
public sealed class TransportApiWebApplicationFactory : WebApplicationFactory<Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.Services.RemoveAll<ILoggerProvider>());
        builder.ConfigureAppConfiguration((_, configuration) => {
            TestConfiguration.Add(configuration);
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=transport_tests;Username=test;Password=test",
                ["OpenAi:ApiKey"] = string.Empty,
            });
        });
        builder.ConfigureTestServices(services => {
            var applicationPartManager = services
                .Single(service => service.ServiceType == typeof(ApplicationPartManager))
                .ImplementationInstance as ApplicationPartManager;
            applicationPartManager?.ApplicationParts.Add(new AssemblyPart(typeof(TestExceptionController).Assembly));
            services.RemoveAll<IHostedService>();
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
        });
    }
}
