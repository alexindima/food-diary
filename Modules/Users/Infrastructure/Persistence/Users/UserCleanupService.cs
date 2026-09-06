using FoodDiary.Infrastructure.Persistence.Shared;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserCleanupService(
    FoodDiaryDbContext dbContext,
    IEnumerable<IUserDataPurgeParticipant> participants,
    ILogger<UserCleanupService> logger) : IUserCleanupService {
    private readonly IReadOnlyList<IUserDataPurgeParticipant> _participants = ValidateParticipants(participants);

    private static IReadOnlyList<IUserDataPurgeParticipant> ValidateParticipants(IEnumerable<IUserDataPurgeParticipant> participants) {
        IUserDataPurgeParticipant[] ordered = [.. participants.OrderBy(participant => participant.Order)];
        if (ordered.Length == 0 || ordered.Select(participant => participant.Order).Distinct().Count() != ordered.Length) {
            throw new InvalidOperationException("User purge requires explicitly composed participants with unique ordering.");
        }
        return ordered;
    }

    public async Task<int> CleanupDeletedUsersAsync(
        DateTime olderThanUtc,
        int batchSize,
        Guid? reassignUserId,
        CancellationToken cancellationToken = default) {
        if (batchSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        }

        SharedTransactionBoundary.EnsureCleanEntry(dbContext);
        UserId? reassignTarget = await ResolveReassignTargetAsync(reassignUserId, cancellationToken).ConfigureAwait(false);
        DateTime thresholdUtc = NormalizeUtc(olderThanUtc);
        IReadOnlyList<UserId> userIds = await GetDeletedUserIdsAsync(thresholdUtc, batchSize, cancellationToken).ConfigureAwait(false);
        int removed = 0;

        foreach (UserId userId in userIds) {
            try {
                if (await CleanupUserAsync(userId, reassignTarget, thresholdUtc, cancellationToken).ConfigureAwait(false)) {
                    removed++;
                }
            } catch (Exception ex) {
                dbContext.ChangeTracker.Clear();
                logger.LogError(ex, "Failed to clean up deleted user {UserId}. Continuing with the next deleted user.", userId.Value);
            }
        }
        return removed;
    }

    private async Task<UserId?> ResolveReassignTargetAsync(Guid? reassignUserId, CancellationToken cancellationToken) {
        if (!reassignUserId.HasValue) {
            return null;
        }

        var targetId = new UserId(reassignUserId.Value);
        bool candidate = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == targetId && u.DeletedAt == null && u.IsActive,
                cancellationToken).ConfigureAwait(false);
        if (candidate) {
            return targetId;
        }

        logger.LogWarning(
            "User cleanup reassign target {UserId} was not found or is not active. Proceeding without reassignment.",
            reassignUserId);
        return null;
    }

    private static DateTime NormalizeUtc(DateTime value) {
        return value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    private async Task<IReadOnlyList<UserId>> GetDeletedUserIdsAsync(DateTime thresholdUtc, int batchSize, CancellationToken cancellationToken) {
        return await dbContext.Users
            .Where(u => u.DeletedAt != null && u.DeletedAt <= thresholdUtc)
            .OrderBy(u => u.DeletedAt)
            .Select(u => u.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<bool> CleanupUserAsync(
        UserId userId,
        UserId? reassignTarget,
        DateTime thresholdUtc,
        CancellationToken cancellationToken) {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => {
            dbContext.ChangeTracker.Clear();
            IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                FormattableString eligibilitySql = $"""
                    SELECT 1 AS "Value"
                    FROM "Users"
                    WHERE "Id" = {userId.Value}
                      AND "DeletedAt" IS NOT NULL
                      AND "DeletedAt" <= {thresholdUtc}
                      AND "IsActive" = FALSE
                    FOR UPDATE
                    """;
                int? eligible = await dbContext.Database
                    .SqlQuery<int>(eligibilitySql)
                    .SingleOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (eligible != 1) {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return false;
                }

                foreach (IUserDataPurgeParticipant participant in _participants) {
                    await participant.PurgeAsync(userId, reassignTarget, cancellationToken).ConfigureAwait(false);
                }
                await DeleteUserRowsAsync(userId, cancellationToken).ConfigureAwait(false);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
        }).ConfigureAwait(false);
    }

    private async Task DeleteUserRowsAsync(UserId userId, CancellationToken cancellationToken) {
        await dbContext.UserRoles.Where(role => role.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
