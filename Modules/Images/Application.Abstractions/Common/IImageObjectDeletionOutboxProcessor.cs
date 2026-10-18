namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageObjectDeletionOutboxProcessor {
    Task<int> ProcessKeysAsync(IReadOnlyList<string> objectKeys, CancellationToken cancellationToken);
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
