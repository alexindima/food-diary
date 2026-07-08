using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodDiary.Infrastructure.Tests.Integration;

[ExcludeFromCodeCoverage]
public sealed class PostgresDatabaseFixture : IAsyncLifetime {
    private PostgreSqlContainer? _container;
    private string? _skipReason;

    public async Task InitializeAsync() {
        if (!DockerAvailability.IsAvailable(out string? reason)) {
            _skipReason = reason;
            return;
        }

        try {
            _container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("fooddiary_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync().ConfigureAwait(false);
        } catch (Exception ex) {
            _skipReason = $"Docker/PostgreSQL test container is unavailable: {ex.Message}";
        }
    }

    public async Task DisposeAsync() {
        if (_container is not null) {
            await _container.DisposeAsync().AsTask().ConfigureAwait(false);
        }
    }

    public async Task<FoodDiaryDbContext> CreateDbContextAsync() {
        FoodDiaryDbContext context = CreateDbContext(await CreateIsolatedDatabaseAsync().ConfigureAwait(false));
        await context.Database.MigrateAsync().ConfigureAwait(false);
        return context;
    }

    public async Task<string> CreateIsolatedDatabaseAsync() {
        EnsureAvailable();

        string databaseName = $"fooddiary_test_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(databaseName).ConfigureAwait(false);

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_container!.GetConnectionString()) {
            Database = databaseName,
        };

        return connectionStringBuilder.ConnectionString;
    }

    public FoodDiaryDbContext CreateDbContext(string connectionString) {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FoodDiaryDbContext(options);
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

    private void EnsureAvailable() {
        if (!string.IsNullOrWhiteSpace(_skipReason)) {
            throw new InvalidOperationException(_skipReason);
        }

        if (_container is null) {
            throw new InvalidOperationException("Docker/PostgreSQL test container was not initialized.");
        }
    }
}
