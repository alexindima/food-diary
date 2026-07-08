using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodDiary.MailInbox.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class MailInboxPostgresFixture : IAsyncLifetime {
    private PostgreSqlContainer? _postgres;
    private string? _skipReason;

    public async Task InitializeAsync() {
        if (!DockerAvailability.IsAvailable(out string? reason)) {
            _skipReason = reason;
            return;
        }

        try {
            _postgres = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("mailinbox_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgres.StartAsync().ConfigureAwait(false);
        } catch (Exception ex) {
            _skipReason = $"Docker/PostgreSQL test container is unavailable: {ex.Message}";
        }
    }

    public async Task DisposeAsync() {
        if (_postgres is not null) {
            await _postgres.DisposeAsync().AsTask().ConfigureAwait(false);
        }
    }

    public async Task<string> CreateIsolatedDatabaseAsync() {
        EnsureAvailable();

        string databaseName = $"mailinbox_test_{Guid.NewGuid():N}";
        var connection = new NpgsqlConnection(_postgres!.GetConnectionString());
        await using (connection.ConfigureAwait(false)) {
            await connection.OpenAsync().ConfigureAwait(false);
            NpgsqlCommand command = connection.CreateCommand();
            await using (command.ConfigureAwait(false)) {
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        var builder = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) {
            Database = databaseName,
        };

        return builder.ConnectionString;
    }

    public void EnsureAvailable() {
        if (!string.IsNullOrWhiteSpace(_skipReason)) {
            throw new InvalidOperationException(_skipReason);
        }

        if (_postgres is null) {
            throw new InvalidOperationException("Docker/PostgreSQL test container was not initialized.");
        }
    }
}
