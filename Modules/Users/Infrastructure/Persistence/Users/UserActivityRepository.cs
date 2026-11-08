using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserActivityRepository(DatabaseFacade database, Func<CancellationToken, Task>? synchronizeTransactionAsync = null)
    : IUserActivityRepository {
    public async Task RecordAsync(UserId userId, DateTime occurredAtUtc, CancellationToken cancellationToken) {
        if (userId.Value == Guid.Empty) {
            throw new ArgumentException("Activity requires a user.", nameof(userId));
        }
        if (occurredAtUtc.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("Activity requires a UTC timestamp.", nameof(occurredAtUtc));
        }
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        DateTime cutoff = occurredAtUtc.AddMinutes(-5);
        await database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UserActivities" ("UserId", "LastActiveAtUtc")
            SELECT "Id", {occurredAtUtc} FROM "Users"
            WHERE "Id" = {userId.Value} AND "IsActive" AND "DeletedAt" IS NULL
            ON CONFLICT ("UserId") DO UPDATE
            SET "LastActiveAtUtc" = EXCLUDED."LastActiveAtUtc"
            WHERE "UserActivities"."LastActiveAtUtc" <= {cutoff}
            """, cancellationToken);
    }
}
