using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public interface IHydrationOperationReceiptRepository {
    Task<HydrationOperationReceipt?> FindAsync(UserId userId, Guid operationId, CancellationToken cancellationToken = default);
    Task AddAsync(HydrationOperationReceipt receipt, CancellationToken cancellationToken = default);
}
