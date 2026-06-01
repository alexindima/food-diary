using FoodDiary.Application.Abstractions.Billing.Common;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Billing;

public sealed class EfBillingTransactionRunner(FoodDiaryDbContext context) : IBillingTransactionRunner {
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => {
            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                await operation(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }
}
