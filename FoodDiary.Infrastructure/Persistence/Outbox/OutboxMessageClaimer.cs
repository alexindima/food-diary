using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal static class OutboxMessageClaimer {
    public static async Task<OutboxClaimBatch<TMessage>> ClaimDueAsync<TMessage>(
        FoodDiaryDbContext context,
        DbSet<TMessage> messages,
        string tableName,
        int batchSize,
        DateTime nowUtc,
        TimeSpan leaseDuration,
        IQueryable<TMessage>? claimedQuery = null,
        CancellationToken cancellationToken = default)
        where TMessage : class, IOutboxMessage {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseDuration, TimeSpan.Zero);

        string workerId = TruncateWorkerId(string.Create(
            CultureInfo.InvariantCulture,
            $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}"));
        DateTime lockedUntilUtc = nowUtc.Add(leaseDuration);

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

    private static async Task<OutboxClaimBatch<TMessage>> ClaimDueWithRelationalDatabaseAsync<TMessage>(
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
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        int reclaimedCount = await strategy.ExecuteAsync(() => ExecuteClaimTransactionAsync(
            context,
            tableName,
            batchSize,
            nowUtc,
            lockedUntilUtc,
            workerId,
            cancellationToken)).ConfigureAwait(false);

        if (reclaimedCount < 0) {
            return new OutboxClaimBatch<TMessage>([], ReclaimedCount: 0);
        }

        IQueryable<TMessage> query = claimedQuery ?? messages;
        List<TMessage> claimed = await query
            .Where(message => EF.Property<string?>(message, "LockedBy") == workerId)
            .OrderBy(message => EF.Property<DateTime>(message, "CreatedOnUtc"))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new OutboxClaimBatch<TMessage>(claimed, reclaimedCount);
    }

    private static async Task<int> ExecuteClaimTransactionAsync(
        FoodDiaryDbContext context,
        string tableName,
        int batchSize,
        DateTime nowUtc,
        DateTime lockedUntilUtc,
        string workerId,
        CancellationToken cancellationToken) {
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
                return -1;
            }

            int reclaimedCount = await CountReclaimedAsync(
                context,
                tableName,
                ids,
                cancellationToken).ConfigureAwait(false);

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
            return reclaimedCount;
        }
    }

    private static async Task<int> CountReclaimedAsync(
        FoodDiaryDbContext context,
        string tableName,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) {
#pragma warning disable EF1002
        return await context.Database
            .SqlQueryRaw<int>(
                $"""
                SELECT COUNT(*)::int AS "Value"
                FROM {tableName}
                WHERE "Id" = ANY(@ids)
                  AND "LockedUntilUtc" IS NOT NULL
                """,
                new NpgsqlParameter<Guid[]>("ids", [.. ids]))
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore EF1002
    }

    private static string ValidateTableName(string tableName) =>
        tableName switch {
            "\"EmailOutbox\"" => tableName,
            "\"ImageObjectDeletionOutbox\"" => tableName,
            "\"NotificationWebPushOutbox\"" => tableName,
            "\"AchievementEvaluationOutbox\"" => tableName,
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unsupported outbox table."),
        };

    private static string TruncateWorkerId(string workerId) =>
        workerId.Length <= 128 ? workerId : workerId[..128];

    private static async Task<OutboxClaimBatch<TMessage>> ClaimDueWithTrackedEntitiesAsync<TMessage>(
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

        int reclaimedCount = 0;
        foreach (TMessage message in claimed) {
            if (message.LockedUntilUtc is not null) {
                reclaimedCount++;
            }

            message.MarkClaimed(lockedUntilUtc, workerId);
        }

        return new OutboxClaimBatch<TMessage>(claimed, reclaimedCount);
    }
}
