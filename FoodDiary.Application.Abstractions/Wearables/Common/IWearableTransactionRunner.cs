namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableTransactionRunner {
    Task<TResult> ExecuteSerializedAsync<TResult>(
        string serializationKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
