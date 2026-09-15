namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageObjectDeletionOutboxProcessor {
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
