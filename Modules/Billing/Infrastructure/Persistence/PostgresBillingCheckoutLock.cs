using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class PostgresBillingCheckoutLock(IModuleSessionLock sessionLock) : IBillingCheckoutLock {
    public Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken = default) {
        long lockKey = BitConverter.ToInt64(userId.ToByteArray(), startIndex: 0);
        return sessionLock.AcquireAsync(lockKey, cancellationToken);
    }
}
