using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.Persistence.Authentication;

public sealed class TelegramOperationStore(IdentityDbContext context, IDataProtectionProvider protectionProvider,
    TimeProvider timeProvider, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : ITelegramOperationStore {
    private const int MaximumPayloadBytes = 32768;
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("FoodDiary.Telegram.Operations.v1");

    public async Task<TelegramOperationId?> RegisterAsync(long botId, long updateId, UserId userId, long securityVersion, string payload,
        CancellationToken cancellationToken) {
        Guid storedUserId = userId.Value;
        if (botId <= 0 || updateId < 0 || storedUserId == Guid.Empty || securityVersion < 0) {
            throw new ArgumentException("Invalid Telegram operation identity.", nameof(userId));
        }
        ValidatePayload(payload);
        var id = Guid.NewGuid();
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        string encrypted = Protect(id, payload);
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "TelegramOperations" ("Id", "BotId", "UpdateId", "UserId", "SecurityVersion", "PayloadHash", "ProtectedPayload", "CreatedAtUtc", "NextAttemptAtUtc", "Completed")
            VALUES ({id}, {botId}, {updateId}, {storedUserId}, {securityVersion}, {hash}, {encrypted}, {now}, {now}, FALSE)
            ON CONFLICT ("BotId", "UpdateId") DO NOTHING
            """, cancellationToken);
        TelegramOperation record = await context.Set<TelegramOperation>().AsNoTracking()
            .SingleAsync(item => item.BotId == botId && item.UpdateId == updateId, cancellationToken);
        return record.UserId == storedUserId && record.SecurityVersion == securityVersion &&
            string.Equals(record.PayloadHash, hash, StringComparison.Ordinal) ? new TelegramOperationId(record.Id) : null;
    }

    public async Task<IReadOnlyList<TelegramOperationId>> ListReadyAsync(long botId, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await ClearCompletedContentAsync(botId, cancellationToken);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        List<Guid> ids = await context.Set<TelegramOperation>().AsNoTracking()
            .Where(item => item.BotId == botId && !item.Completed && item.NextAttemptAtUtc <= now &&
                (item.LeaseExpiresAtUtc == null || item.LeaseExpiresAtUtc <= now))
            .OrderBy(item => item.NextAttemptAtUtc).ThenBy(item => item.Id).Take(50)
            .Select(item => item.Id).ToListAsync(cancellationToken);
        return ids.Select(id => new TelegramOperationId(id)).ToArray();
    }

    public async Task<TelegramOperationLease?> AcquireAsync(long botId, TelegramOperationId operationId, CancellationToken cancellationToken) {
        Guid storedOperationId = operationId.Value;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        DateTime expires = now.AddMinutes(2);
        var leaseId = Guid.NewGuid();
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        int changed = await context.Set<TelegramOperation>()
            .Where(item => item.Id == storedOperationId && item.BotId == botId && !item.Completed && item.NextAttemptAtUtc <= now &&
                (item.LeaseExpiresAtUtc == null || item.LeaseExpiresAtUtc <= now))
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.LeaseId, leaseId)
                .SetProperty(item => item.LeaseExpiresAtUtc, expires), cancellationToken);
        if (changed != 1) {
            return null;
        }
        return await GetLeaseAsync(botId, operationId, new TelegramLeaseId(leaseId), cancellationToken);
    }

    public async Task<TelegramOperationLease?> GetLeaseAsync(long botId, TelegramOperationId operationId, TelegramLeaseId leaseId, CancellationToken cancellationToken) {
        Guid storedOperationId = operationId.Value;
        Guid storedLeaseId = leaseId.Value;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        TelegramOperation? record = await context.Set<TelegramOperation>().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == storedOperationId && item.BotId == botId && item.LeaseId == storedLeaseId &&
                !item.Completed && item.LeaseExpiresAtUtc > now,
                cancellationToken);
        if (record is null) {
            return null;
        }
        return new TelegramOperationLease(new TelegramOperationId(record.Id), leaseId, new UserId(record.UserId), record.SecurityVersion,
            Unprotect(record.Id, record.ProtectedPayload),
            record.ProtectedCheckpoint is null ? null : Unprotect(record.Id, record.ProtectedCheckpoint), record.LeaseExpiresAtUtc!.Value, record.CreatedAtUtc);
    }

    public async Task<bool> CheckpointAsync(long botId, TelegramOperationId operationId, TelegramLeaseId leaseId, string checkpoint, bool completed,
        DateTime nextAttemptAtUtc, CancellationToken cancellationToken) {
        Guid storedOperationId = operationId.Value;
        Guid storedLeaseId = leaseId.Value;
        ValidatePayload(checkpoint);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (nextAttemptAtUtc.Kind != DateTimeKind.Utc || nextAttemptAtUtc > now.AddDays(1)) {
            throw new ArgumentException("Invalid retry time.", nameof(nextAttemptAtUtc));
        }
        string? encrypted = completed ? null : Protect(storedOperationId, checkpoint);
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await context.Set<TelegramOperation>()
            .Where(item => item.Id == storedOperationId && item.BotId == botId && !item.Completed && item.LeaseId == storedLeaseId && item.LeaseExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ProtectedCheckpoint, encrypted)
                .SetProperty(item => item.Completed, completed).SetProperty(item => item.NextAttemptAtUtc, nextAttemptAtUtc)
                .SetProperty(item => item.LeaseId, (Guid?)null).SetProperty(item => item.LeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(item => item.ProtectedPayload, item => completed ? string.Empty : item.ProtectedPayload),
                cancellationToken) == 1;
    }

    public async Task<bool> ResumeAsync(long botId, TelegramOperationId operationId, UserId userId, long securityVersion,
        string expectedCheckpoint, string checkpoint, CancellationToken cancellationToken) {
        ValidatePayload(expectedCheckpoint);
        ValidatePayload(checkpoint);
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        DateTime cutoff = now.AddMinutes(-15);
        Guid id = operationId.Value;
        Guid ownerId = userId.Value;
        IQueryable<TelegramOperation> pending = context.Set<TelegramOperation>()
            .Where(item => item.Id == id && item.BotId == botId && item.UserId == ownerId && item.SecurityVersion == securityVersion &&
                !item.Completed && item.CreatedAtUtc > cutoff);
        string replacement = Protect(id, checkpoint);
        // A worker can reschedule the same offer as the user clicks. Re-read once if its checkpoint wins the race.
        for (int attempt = 0; attempt < 2; attempt++) {
            string? current = await pending.Select(item => item.ProtectedCheckpoint).SingleOrDefaultAsync(cancellationToken);
            if (current is null || !string.Equals(Unprotect(id, current), expectedCheckpoint, StringComparison.Ordinal)) {
                return false;
            }
            int changed = await pending.Where(item => item.ProtectedCheckpoint == current)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ProtectedCheckpoint, replacement)
                    .SetProperty(item => item.NextAttemptAtUtc, now)
                    .SetProperty(item => item.LeaseId, (Guid?)null).SetProperty(item => item.LeaseExpiresAtUtc, (DateTime?)null),
                    cancellationToken);
            if (changed == 1) {
                return true;
            }
        }
        return false;
    }

    public async Task<bool> HasPendingUserAsync(UserId userId, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await context.Set<TelegramOperation>().AnyAsync(item => item.UserId == userId.Value && !item.Completed, cancellationToken);
    }

    public async Task CancelUserAsync(UserId userId, CancellationToken cancellationToken) {
        Guid storedUserId = userId.Value;
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await context.Set<TelegramOperation>().Where(item => item.UserId == storedUserId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Completed, valueExpression: true)
                .SetProperty(item => item.ProtectedPayload, string.Empty).SetProperty(item => item.ProtectedCheckpoint, (string?)null)
                .SetProperty(item => item.LeaseId, (Guid?)null).SetProperty(item => item.LeaseExpiresAtUtc, (DateTime?)null), cancellationToken);
    }

    private async Task ClearCompletedContentAsync(long botId, CancellationToken cancellationToken) {
        // Keep the deduplication identity; only terminal private content is no longer needed.
        // Bounded batches also cover checkpoints written before immediate erasure was introduced.
        List<Guid> ids = await context.Set<TelegramOperation>().AsNoTracking()
            .Where(item => item.BotId == botId && item.Completed &&
                (item.ProtectedPayload != string.Empty || item.ProtectedCheckpoint != null))
            .OrderBy(item => item.Id).Take(100).Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (ids.Count == 0) {
            return;
        }
        await context.Set<TelegramOperation>().Where(item => item.BotId == botId && item.Completed && ids.Contains(item.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ProtectedPayload, string.Empty)
                .SetProperty(item => item.ProtectedCheckpoint, (string?)null), cancellationToken);
    }

    public async Task CancelOperationAsync(long botId, TelegramOperationId operationId, CancellationToken cancellationToken) {
        Guid storedOperationId = operationId.Value;
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await context.Set<TelegramOperation>().Where(item => item.BotId == botId && item.Id == storedOperationId && !item.Completed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Completed, valueExpression: true)
                .SetProperty(item => item.ProtectedPayload, string.Empty).SetProperty(item => item.ProtectedCheckpoint, (string?)null)
                .SetProperty(item => item.LeaseId, (Guid?)null).SetProperty(item => item.LeaseExpiresAtUtc, (DateTime?)null), cancellationToken);
    }

    private string Protect(Guid operationId, string payload) => _protector.CreateProtector(operationId.ToString("N")).Protect(payload);
    private string Unprotect(Guid operationId, string payload) => _protector.CreateProtector(operationId.ToString("N")).Unprotect(payload);
    private static void ValidatePayload(string payload) {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        if (Encoding.UTF8.GetByteCount(payload) > MaximumPayloadBytes) {
            throw new ArgumentException("Telegram operation payload is too large.", nameof(payload));
        }
    }
}
