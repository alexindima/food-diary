namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingCheckoutLock {
    Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken = default);
}
