namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageObjectDeletionOutboxProcessor {
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
