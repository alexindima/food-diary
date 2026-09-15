namespace FoodDiary.Modules.Wearables.Application.Abstractions.Common;

public interface IWearableTransactionRunner {
    Task<TResult> ExecuteSerializedAsync<TResult>(
        string serializationKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
