namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingCheckoutLock {
    Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken = default);
}
