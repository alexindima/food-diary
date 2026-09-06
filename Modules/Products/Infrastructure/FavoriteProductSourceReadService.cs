using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Infrastructure;

internal sealed class FavoriteProductSourceReadService(IProductLookupService source) : IFavoriteProductSourceReadService {
    public async Task<Result<FavoriteProductSourceModel>> GetAccessibleAsync(ProductId id, UserId userId, CancellationToken cancellationToken = default) {
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> products = await source.GetAccessibleByIdsAsync([id], userId, cancellationToken).ConfigureAwait(false);
        ProductOverviewReadItem? item = products.GetValueOrDefault(id);
        return item is null
            ? Result.Failure<FavoriteProductSourceModel>(ProductErrors.NotFound(id.Value))
            : Result.Success(new FavoriteProductSourceModel(
            item.Name,
            item.Brand,
            item.Barcode,
            item.Comment,
            item.ImageUrl,
            item.CaloriesPerBase,
            item.ProteinsPerBase,
            item.FatsPerBase,
            item.CarbsPerBase,
            item.FiberPerBase,
            item.AlcoholPerBase,
            item.QualityScore,
            item.QualityGrade,
            item.IsOwnedByCurrentUser,
            item.BaseUnit,
            item.DefaultPortionAmount));
    }
}
