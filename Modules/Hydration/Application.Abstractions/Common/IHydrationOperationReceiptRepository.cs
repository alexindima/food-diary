using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Abstractions.Common;

public interface IHydrationOperationReceiptRepository {
    Task<HydrationOperationReceipt?> FindAsync(UserId userId, Guid operationId, CancellationToken cancellationToken = default);
    Task AddAsync(HydrationOperationReceipt receipt, CancellationToken cancellationToken = default);
}
