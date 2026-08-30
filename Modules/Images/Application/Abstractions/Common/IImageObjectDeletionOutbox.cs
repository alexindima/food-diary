namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageObjectDeletionOutbox {
    Task EnqueueAsync(string objectKey, bool isConfirmed, CancellationToken cancellationToken = default);

    Task EnqueueAsync(string objectKey, CancellationToken cancellationToken = default) =>
        EnqueueAsync(objectKey, isConfirmed: true, cancellationToken);
}
