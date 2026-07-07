using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal static class OutboxMessageClaimer {
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public static async Task<List<TMessage>> ClaimDueAsync<TMessage>(
        FoodDiaryDbContext context,
        DbSet<TMessage> messages,
        string tableName,
        int batchSize,
        DateTime nowUtc,
        IQueryable<TMessage>? claimedQuery = null,
        CancellationToken cancellationToken = default)
        where TMessage : class, IOutboxMessage {
        string workerId = TruncateWorkerId(string.Create(
            CultureInfo.InvariantCulture,
            $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}"));
        DateTime lockedUntilUtc = nowUtc.Add(LeaseDuration);

        if (!context.Database.IsRelational()) {
            return await ClaimDueWithTrackedEntitiesAsync(
                messages,
                batchSize,
                nowUtc,
                lockedUntilUtc,
                workerId,
                cancellationToken).ConfigureAwait(false);
        }

        return await ClaimDueWithRelationalDatabaseAsync(
            context,
            messages,
            ValidateTableName(tableName),
            batchSize,
            nowUtc,
            lockedUntilUtc,
            workerId,
            claimedQuery,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<TMessage>> ClaimDueWithRelationalDatabaseAsync<TMessage>(
        FoodDiaryDbContext context,
        DbSet<TMessage> messages,
        string tableName,
        int batchSize,
        DateTime nowUtc,
        DateTime lockedUntilUtc,
        string workerId,
        IQueryable<TMessage>? claimedQuery,
        CancellationToken cancellationToken)
        where TMessage : class, IOutboxMessage {
        IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false)) {
#pragma warning disable EF1002
            IReadOnlyList<Guid> ids = await context.Database
                .SqlQueryRaw<Guid>(
                    $"""
                    SELECT "Id" AS "Value"
                    FROM {tableName}
                    WHERE "ProcessedOnUtc" IS NULL
                      AND "DeadLetteredOnUtc" IS NULL
                      AND "NextAttemptOnUtc" <= @nowUtc
                      AND ("LockedUntilUtc" IS NULL OR "LockedUntilUtc" <= @nowUtc)
                    ORDER BY "CreatedOnUtc"
                    LIMIT @batchSize
                    FOR UPDATE SKIP LOCKED
                    """,
                    new NpgsqlParameter<DateTime>("nowUtc", nowUtc),
                    new NpgsqlParameter<int>("batchSize", batchSize))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
#pragma warning restore EF1002

            if (ids.Count == 0) {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return [];
            }

#pragma warning disable EF1002
            await context.Database
                .ExecuteSqlRawAsync(
                    $"""
                    UPDATE {tableName}
                    SET "LockedUntilUtc" = @lockedUntilUtc, "LockedBy" = @workerId
                    WHERE "Id" = ANY(@ids)
                    """,
                    [
                        new NpgsqlParameter<DateTime>("lockedUntilUtc", lockedUntilUtc),
                        new NpgsqlParameter<string>("workerId", workerId),
                        new NpgsqlParameter<Guid[]>("ids", [.. ids]),
                    ],
                    cancellationToken)
                .ConfigureAwait(false);
#pragma warning restore EF1002

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        IQueryable<TMessage> query = claimedQuery ?? messages;
        return await query
            .Where(message => EF.Property<string?>(message, "LockedBy") == workerId)
            .OrderBy(message => EF.Property<DateTime>(message, "CreatedOnUtc"))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string ValidateTableName(string tableName) =>
        tableName switch {
            "\"EmailOutbox\"" => tableName,
            "\"ImageObjectDeletionOutbox\"" => tableName,
            "\"NotificationWebPushOutbox\"" => tableName,
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unsupported outbox table."),
        };

    private static string TruncateWorkerId(string workerId) =>
        workerId.Length <= 128 ? workerId : workerId[..128];

    private static async Task<List<TMessage>> ClaimDueWithTrackedEntitiesAsync<TMessage>(
        DbSet<TMessage> messages,
        int batchSize,
        DateTime nowUtc,
        DateTime lockedUntilUtc,
        string workerId,
        CancellationToken cancellationToken)
        where TMessage : class, IOutboxMessage {
        List<TMessage> claimed = await messages
            .Where(message =>
                EF.Property<DateTime?>(message, "ProcessedOnUtc") == null &&
                EF.Property<DateTime?>(message, "DeadLetteredOnUtc") == null &&
                EF.Property<DateTime>(message, "NextAttemptOnUtc") <= nowUtc &&
                (EF.Property<DateTime?>(message, "LockedUntilUtc") == null ||
                 EF.Property<DateTime?>(message, "LockedUntilUtc") <= nowUtc))
            .OrderBy(message => EF.Property<DateTime>(message, "CreatedOnUtc"))
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (TMessage message in claimed) {
            message.MarkClaimed(lockedUntilUtc, workerId);
        }

        return claimed;
    }
}
