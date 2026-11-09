using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

internal sealed class UserPremiumTrialStore(UsersDbContext context, IModuleTransactionCoordinator coordinator) : IUserPremiumTrialStore {
    public async Task LockAccountAsync(UserId userId, CancellationToken cancellationToken) {
        await JoinGrantTransactionAsync(cancellationToken);
        _ = await context.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value" FROM "Users" WHERE "Id" = {userId.Value} FOR UPDATE
            """).ToArrayAsync(cancellationToken);
    }

    public async Task<bool> HasRedeemedTelegramAsync(long telegramUserId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        return await context.Database.SqlQuery<bool>($"""
            SELECT EXISTS (SELECT 1 FROM "TelegramPremiumTrialRedemptions" WHERE "TelegramUserId" = {telegramUserId}) AS "Value"
            """).SingleAsync(cancellationToken);
    }

    public async Task<bool> TryRedeemTelegramAsync(long telegramUserId, CancellationToken cancellationToken) {
        await JoinGrantTransactionAsync(cancellationToken);
        int inserted = await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "TelegramPremiumTrialRedemptions" ("TelegramUserId") VALUES ({telegramUserId})
            ON CONFLICT ("TelegramUserId") DO NOTHING
            """, cancellationToken);
        return inserted == 1;
    }

    private Task JoinGrantTransactionAsync(CancellationToken cancellationToken) => context.Database.UseTransactionAsync(
        coordinator.CurrentTransaction ?? throw new InvalidOperationException("Premium trial grants require a caller transaction."), cancellationToken);
}
