using FoodDiary.Application.Images.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program> {
    private readonly string _databaseName = $"fooddiary-tests-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) => {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
                ["Jwt:Issuer"] = "fooddiary-tests",
                ["Jwt:Audience"] = "fooddiary-tests",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "30",
                ["S3:AccessKeyId"] = "test-access-key",
                ["S3:SecretAccessKey"] = "test-secret-key",
                ["S3:Region"] = "us-east-1",
                ["S3:Bucket"] = "fooddiary-integration-tests",
                ["S3:ServiceUrl"] = "https://s3.test.local",
                ["S3:PublicBaseUrl"] = "https://cdn.test.local",
            });
        });

        builder.ConfigureServices(services => {
            services.RemoveAll<DbContextOptions<FoodDiaryDbContext>>();
            services.RemoveAll<FoodDiaryDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<FoodDiaryDbContext>>();
            services.RemoveAll<IImageStorageService>();

            services.AddDbContext<FoodDiaryDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));
            services.AddSingleton<IImageStorageService, TestImageStorageService>();

            var applicationPartManager = services
                .Single(service => service.ServiceType == typeof(ApplicationPartManager))
                .ImplementationInstance as ApplicationPartManager;

            applicationPartManager?.ApplicationParts.Add(new AssemblyPart(typeof(TestExceptionController).Assembly));
        });
    }
}
