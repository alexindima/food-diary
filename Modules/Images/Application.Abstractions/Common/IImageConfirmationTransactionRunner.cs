using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageConfirmationTransactionRunner {
    Task<T> ExecuteSerializedAsync<T>(ImageAssetId assetId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
