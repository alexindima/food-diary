using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Locking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class PostgresBillingCheckoutLock(FoodDiaryDbContext context) : IBillingCheckoutLock {
    public async Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken = default) {
        long lockKey = BitConverter.ToInt64(userId.ToByteArray(), startIndex: 0);
        string connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The billing checkout lock requires a relational connection string.");
        return await PostgresAdvisoryLockLease
            .AcquireAsync(connectionString, lockKey, cancellationToken)
            .ConfigureAwait(false);
    }
}
