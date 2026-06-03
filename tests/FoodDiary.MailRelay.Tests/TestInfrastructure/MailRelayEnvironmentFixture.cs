using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class MailRelayEnvironmentFixture : IAsyncLifetime {
    private PostgreSqlContainer? _postgres;
    private RabbitMqContainer? _rabbitMq;
    private string? _skipReason;

    public string PostgresConnectionString => _postgres?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container is not available.");

    public string RabbitMqConnectionString => _rabbitMq?.GetConnectionString()
        ?? throw new InvalidOperationException("RabbitMQ container is not available.");

    public string RabbitMqHostName => _rabbitMq?.Hostname
        ?? throw new InvalidOperationException("RabbitMQ container is not available.");

    public ushort RabbitMqPort => (ushort)(_rabbitMq?.GetMappedPublicPort(5672)
        ?? throw new InvalidOperationException("RabbitMQ container is not available."));

    public async Task InitializeAsync() {
        if (!DockerAvailability.IsAvailable(out var reason)) {
            _skipReason = reason;
            return;
        }

        try {
            _postgres = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("mailrelay_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management")
                .WithUsername("guest")
                .WithPassword("guest")
                .Build();

            await _postgres.StartAsync().ConfigureAwait(false);
            await _rabbitMq.StartAsync().ConfigureAwait(false);
        } catch (Exception ex) {
            _skipReason = $"Docker test containers are unavailable: {ex.Message}";
        }
    }

    public async Task DisposeAsync() {
        if (_rabbitMq is not null) {
            await _rabbitMq.DisposeAsync().AsTask().ConfigureAwait(false);
        }

        if (_postgres is not null) {
            await _postgres.DisposeAsync().AsTask().ConfigureAwait(false);
        }
    }

    public void EnsureAvailable() {
        if (!string.IsNullOrWhiteSpace(_skipReason)) {
            throw new InvalidOperationException(_skipReason);
        }

        if (_postgres is null || _rabbitMq is null) {
            throw new InvalidOperationException("MailRelay test containers were not initialized.");
        }
    }

    public async Task<string> CreateIsolatedDatabaseAsync() {
        EnsureAvailable();

        var databaseName = $"mailrelay_test_{Guid.NewGuid():N}";
        var connection = new NpgsqlConnection(PostgresConnectionString);
        await using (connection.ConfigureAwait(false)) {
            await connection.OpenAsync().ConfigureAwait(false);
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false)) {
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString) {
                    Database = databaseName
                };

                return builder.ConnectionString;
            }
        }
    }
}
