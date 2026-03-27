using FoodDiary.Application.Images.Common;
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

    public async Task InitializeAsync() {
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
            });
        });

        builder.ConfigureServices(services => {
            services.RemoveAll<DbContextOptions<FoodDiaryDbContext>>();
            services.RemoveAll<FoodDiaryDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<FoodDiaryDbContext>>();
            services.RemoveAll<IImageStorageService>();

            services.AddDbContext<FoodDiaryDbContext>(options =>
                options.UseNpgsql(GetRequiredConnectionString()));
            services.AddSingleton<IImageStorageService, TestImageStorageService>();
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
