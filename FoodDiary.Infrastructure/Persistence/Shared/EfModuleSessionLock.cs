using FoodDiary.Infrastructure.Persistence.Locking;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Shared;

internal sealed class EfModuleSessionLock(FoodDiaryDbContext context) : IModuleSessionLock {
    public async Task<IAsyncDisposable> AcquireAsync(long lockKey, CancellationToken cancellationToken = default) {
        string connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The session lock requires a relational connection string.");
        return await PostgresAdvisoryLockLease
            .AcquireAsync(connectionString, lockKey, cancellationToken)
            .ConfigureAwait(false);
    }
}
