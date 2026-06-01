using FoodDiary.MailInbox.Application.Abstractions;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlMailInboxReadinessChecker(NpgsqlDataSource dataSource) : IMailInboxReadinessChecker {
    public async Task CheckReadyAsync(CancellationToken cancellationToken) {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand("select 1", connection);
            await using (command.ConfigureAwait(false)) {
                await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
