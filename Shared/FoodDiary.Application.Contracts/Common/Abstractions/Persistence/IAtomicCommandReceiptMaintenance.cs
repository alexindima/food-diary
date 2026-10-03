namespace FoodDiary.Application.Contracts.Common.Abstractions.Persistence;

public interface IAtomicCommandReceiptMaintenance {
    Task<int> DeleteExpiredBatchAsync(int batchSize, CancellationToken cancellationToken = default);
    Task DeleteOwnerAsync(Guid userId, CancellationToken cancellationToken = default);
}
