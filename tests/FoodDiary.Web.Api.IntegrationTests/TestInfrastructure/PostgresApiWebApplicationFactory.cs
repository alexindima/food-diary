using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class PostgresApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime {
    private PostgreSqlContainer? _container;
    private string? _connectionString;

    public TestEmailSender EmailSender { get; } = new();

    public async Task InitializeAsync() {
        if (!DockerAvailability.IsAvailable(out _)) {
            return;
        }

        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("fooddiary_api_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync().ConfigureAwait(false);

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) {
            Database = $"fooddiary_api_test_{Guid.NewGuid():N}",
        };

        await CreateDatabaseAsync(connectionStringBuilder.Database).ConfigureAwait(false);
        _connectionString = connectionStringBuilder.ConnectionString;
    }

    public new async Task DisposeAsync() {
        EmailSender.Clear();
        await base.DisposeAsync().ConfigureAwait(false);
        if (_container is not null) {
            await _container.DisposeAsync().AsTask().ConfigureAwait(false);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.Services.RemoveAll<ILoggerProvider>());
        builder.ConfigureAppConfiguration((_, configBuilder) => TestConfiguration.Add(configBuilder));

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
            services.AddSingleton(EmailSender);
            services.AddSingleton<IEmailSender>(EmailSender);
            services.AddSingleton<IPasswordHasher, TestPasswordHasher>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder) {
        IHost host = base.CreateHost(builder);

        using IServiceScope scope = host.Services.CreateScope();
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        dbContext.Database.Migrate();

        return host;
    }

    private async Task CreateDatabaseAsync(string databaseName) {
        var connection = new NpgsqlConnection(_container!.GetConnectionString());
        await using (connection.ConfigureAwait(false)) {
            await connection.OpenAsync().ConfigureAwait(false);

            NpgsqlCommand command = connection.CreateCommand();
            await using (command.ConfigureAwait(false)) {
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }

    private string GetRequiredConnectionString() =>
        _connectionString ?? throw new InvalidOperationException("PostgreSQL integration database was not initialized.");
}
