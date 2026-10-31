using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class MailRelayReadinessChecker(NpgsqlDataSource dataSource) : IMailRelayReadinessChecker {
    public async Task CheckReadyAsync(CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using (connection) {
            var command = new NpgsqlCommand("select 1", connection);
            await using (command) {
                await command.ExecuteScalarAsync(cancellationToken);
            }
        }
    }
}
