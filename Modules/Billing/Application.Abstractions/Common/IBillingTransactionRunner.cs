namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingTransactionRunner {
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    Task ExecuteSerializedAsync(
        string serializationKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
