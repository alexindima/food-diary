using System.Net;
using System.Security.Cryptography;
using Docker.DotNet.Models;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

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
            string password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("fooddiary_tests")
                .WithUsername("postgres")
                .WithPassword(password)
                .WithCreateParameterModifier(parameters => {
                    parameters.HostConfig ??= new HostConfig();
                    parameters.HostConfig.PortBindings ??= new Dictionary<string, IList<PortBinding>>(StringComparer.Ordinal);
                    parameters.HostConfig.PortBindings["5432/tcp"] = [
                        new PortBinding {
                            HostIP = IPAddress.Loopback.ToString(),
                            HostPort = "0",
                        },
                    ];
                })
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
            Pooling = false,
        };

        return connectionStringBuilder.ConnectionString;
    }

    public FoodDiaryDbContext CreateDbContext(string connectionString, bool enableRetries = false) {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(
                connectionString,
                npgsqlOptions => {
                    if (enableRetries) {
                        npgsqlOptions.EnableRetryOnFailure();
                    }
                })
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
