using FoodDiary.MailInbox.Application.Abstractions;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlMailInboxReadinessChecker(NpgsqlDataSource dataSource) : IMailInboxReadinessChecker {
    public async Task CheckReadyAsync(CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            const string sql = """
                               select to_regclass('public.mailinbox_messages') is not null
                                  and to_regclass('public.mailinbox_schema_migrations') is not null
                                  and exists (
                                      select 1
                                      from information_schema.columns
                                      where table_schema = 'public'
                                        and table_name = 'mailinbox_messages'
                                        and column_name = 'read_at_utc'
                                  );
                               """;
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result is not true) {
                    throw new InvalidOperationException("MailInbox schema is not ready: required schema objects are missing.");
                }
            }
        }
    }
}
