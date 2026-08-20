using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class TestAuthApiWebApplicationFactory : WebApplicationFactory<Program> {
    private readonly string _databaseName = $"fooddiary-tests-auth-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.Services.RemoveAll<ILoggerProvider>());
        builder.ConfigureAppConfiguration((_, configBuilder) => TestConfiguration.Add(configBuilder));

        builder.ConfigureServices(services => {
            services.RemoveAll<DbContextOptions<FoodDiaryDbContext>>();
            services.RemoveAll<FoodDiaryDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<FoodDiaryDbContext>>();
            services.RemoveAll<IImageStorageService>();
            services.RemoveAll<IPasswordHasher>();

            services.AddDbContext<FoodDiaryDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));
            services.AddSingleton<IImageStorageService, TestImageStorageService>();
            services.AddSingleton<IPasswordHasher, TestPasswordHasher>();
        });

        builder.ConfigureTestServices(services => {
            services
                .AddAuthentication(options => {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }
}
