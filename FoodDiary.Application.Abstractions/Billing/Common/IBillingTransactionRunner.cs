namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingTransactionRunner {
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
