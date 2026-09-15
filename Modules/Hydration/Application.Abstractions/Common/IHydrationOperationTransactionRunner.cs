using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Abstractions.Common;

public interface IHydrationOperationTransactionRunner {
    Task<T> ExecuteSerializedAsync<T>(UserId userId, Guid operationId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
