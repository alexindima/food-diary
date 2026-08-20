using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program> {
    private readonly string _databaseName = $"fooddiary-tests-{Guid.NewGuid():N}";
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

            var applicationPartManager = services
                .Single(service => service.ServiceType == typeof(ApplicationPartManager))
                .ImplementationInstance as ApplicationPartManager;

            applicationPartManager?.ApplicationParts.Add(new AssemblyPart(typeof(TestExceptionController).Assembly));
        });
    }
}
