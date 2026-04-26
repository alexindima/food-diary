using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

public sealed class TestAuthApiWebApplicationFactory : WebApplicationFactory<Program> {
    private readonly string _databaseName = $"fooddiary-tests-auth-{Guid.NewGuid():N}";
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
                ["Stripe:SecretKey"] = "sk_test_codex",
                ["Stripe:WebhookSecret"] = "whsec_codex",
                ["Stripe:PremiumMonthlyPriceId"] = "price_monthly_codex",
                ["Stripe:PremiumYearlyPriceId"] = "price_yearly_codex",
                ["Stripe:SuccessUrl"] = "https://example.com/billing/success",
                ["Stripe:CancelUrl"] = "https://example.com/billing/cancel",
                ["Stripe:PortalReturnUrl"] = "https://example.com/settings/billing",
                ["RateLimiting:Auth:PermitLimit"] = "1000",
                ["RateLimiting:Auth:WindowSeconds"] = "60",
                ["RateLimiting:Ai:PermitLimit"] = "1000",
                ["RateLimiting:Ai:WindowSeconds"] = "60",
            });
        });

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
