using FoodDiary.Modules.Images.Domain.ValueObjects;
namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageObjectDeletionOutbox {
    Task EnqueueAsync(ObjectStorageKey key, bool isConfirmed, CancellationToken cancellationToken = default);

    Task EnqueueAsync(ObjectStorageKey key, CancellationToken cancellationToken = default) =>
        EnqueueAsync(key, isConfirmed: true, cancellationToken);
}
