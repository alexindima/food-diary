using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Users;

internal sealed class TelegramIdentityConflictInterceptor : SaveChangesInterceptor {
    public override void SaveChangesFailed(DbContextErrorEventData eventData) => Translate(eventData.Exception);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default) {
        Translate(eventData.Exception);
        return Task.CompletedTask;
    }

    internal static void Translate(Exception exception) {
        if (exception is DbUpdateException { InnerException: PostgresException postgres } &&
            string.Equals(postgres.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal) &&
            postgres.ConstraintName is "IX_Users_TelegramUserId" or "IX_Users_TelegramOidcIssuer_TelegramOidcSubject") {
            // Do not retain provider details, which may include account identifiers.
            throw new DbUpdateConcurrencyException("The Telegram identity was linked by another request. Start sign-in again.");
        }
    }
}
