using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodDiary.Infrastructure.Tests.Integration;

public sealed class PostgresDatabaseFixture : IAsyncLifetime {
    private PostgreSqlContainer? _container;
    private string? _skipReason;

    public async Task InitializeAsync() {
        try {
            _container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("fooddiary_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();
        } catch (Exception ex) {
            _skipReason = $"Docker/PostgreSQL test container is unavailable: {ex.Message}";
        }
    }

    public async Task DisposeAsync() {
        if (_container is not null) {
            await _container.DisposeAsync().AsTask();
        }
    }

    public async Task<FoodDiaryDbContext> CreateDbContextAsync() {
        EnsureAvailable();

        var databaseName = $"fooddiary_test_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(databaseName);

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_container!.GetConnectionString()) {
            Database = databaseName
        };

        var options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(connectionStringBuilder.ConnectionString)
            .Options;

        var context = new FoodDiaryDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    private async Task CreateDatabaseAsync(string databaseName) {
        await using var connection = new NpgsqlConnection(_container!.GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();
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

[CollectionDefinition("postgres-database")]
public sealed class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture> {
    public const string Name = "postgres-database";
}
