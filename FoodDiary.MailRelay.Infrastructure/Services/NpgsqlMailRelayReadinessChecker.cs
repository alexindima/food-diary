using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class NpgsqlMailRelayReadinessChecker(NpgsqlDataSource dataSource) : IMailRelayReadinessChecker {
    public async Task CheckReadyAsync(CancellationToken cancellationToken) {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("select 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
