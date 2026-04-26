using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

public sealed class PostgresApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime {
    private PostgreSqlContainer? _container;
    private string? _connectionString;
    private readonly TestEmailSender _testEmailSender = new();

    public TestEmailSender EmailSender => _testEmailSender;

    public async Task InitializeAsync() {
        if (!DockerAvailability.IsAvailable(out _)) {
            return;
        }

        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("fooddiary_api_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) {
            Database = $"fooddiary_api_test_{Guid.NewGuid():N}"
        };

        await CreateDatabaseAsync(connectionStringBuilder.Database);
        _connectionString = connectionStringBuilder.ConnectionString;
    }

    public new async Task DisposeAsync() {
        _testEmailSender.Clear();
        base.Dispose();
        if (_container is not null) {
            await _container.DisposeAsync().AsTask();
        }
    }

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
            services.RemoveAll<IEmailSender>();
            services.RemoveAll<TestEmailSender>();
            services.RemoveAll<IPasswordHasher>();

            services.AddDbContext<FoodDiaryDbContext>(options =>
                options.UseNpgsql(GetRequiredConnectionString()));
            services.AddSingleton<IImageStorageService, TestImageStorageService>();
            services.AddSingleton(_testEmailSender);
            services.AddSingleton<IEmailSender>(_testEmailSender);
            services.AddSingleton<IPasswordHasher, TestPasswordHasher>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder) {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        dbContext.Database.Migrate();

        return host;
    }

    private async Task CreateDatabaseAsync(string databaseName) {
        await using var connection = new NpgsqlConnection(_container!.GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();
    }

    private string GetRequiredConnectionString() =>
        _connectionString ?? throw new InvalidOperationException("PostgreSQL integration database was not initialized.");
}
