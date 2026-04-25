using FoodDiary.MailInbox.Application.Abstractions;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlMailInboxReadinessChecker(NpgsqlDataSource dataSource) : IMailInboxReadinessChecker {
    public async Task CheckReadyAsync(CancellationToken cancellationToken) {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("select 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
