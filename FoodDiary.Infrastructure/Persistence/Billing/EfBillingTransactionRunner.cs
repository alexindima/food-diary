using FoodDiary.Application.Abstractions.Billing.Common;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Billing;

public sealed class EfBillingTransactionRunner(FoodDiaryDbContext context) : IBillingTransactionRunner {
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
