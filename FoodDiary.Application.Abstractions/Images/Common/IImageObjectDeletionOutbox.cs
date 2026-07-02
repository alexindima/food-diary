namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageObjectDeletionOutbox {
    Task EnqueueAsync(string objectKey, CancellationToken cancellationToken = default);
}
