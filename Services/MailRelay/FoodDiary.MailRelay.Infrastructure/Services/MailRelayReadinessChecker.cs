using Npgsql;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class MailRelayReadinessChecker(
    NpgsqlDataSource dataSource,
    RabbitMqMailRelayBroker broker,
    IOptions<MailRelayBrokerOptions> brokerOptions) : IMailRelayReadinessChecker {
    private readonly MailRelayBrokerOptions _brokerOptions = brokerOptions.Value;

    public async Task CheckReadyAsync(CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand("select 1", connection);
            await using (command.ConfigureAwait(false)) {
                await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (string.Equals(_brokerOptions.Backend, MailRelayBrokerOptions.RabbitMqBackend, StringComparison.Ordinal)) {
            await broker.CheckReadyAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
