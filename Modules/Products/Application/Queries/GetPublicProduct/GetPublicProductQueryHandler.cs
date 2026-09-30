using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Queries.GetPublicProduct;

public sealed class GetPublicProductQueryHandler(IProductOverviewReadService products)
    : IQueryHandler<GetPublicProductQuery, Result<PublicProductModel>> {
    public async Task<Result<PublicProductModel>> Handle(GetPublicProductQuery query, CancellationToken cancellationToken) {
        ProductId id = query.ProductId;
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> rows = await products.GetByIdsWithUsageAsync([id], UserId.Empty, includePublic: true, cancellationToken).ConfigureAwait(false);
        if (!rows.TryGetValue(id, out ProductOverviewReadItem? product) || product.Visibility != Visibility.Public) {
            return Result.Failure<PublicProductModel>(ProductErrors.NotAccessible(query.ProductId.Value));
        }
        return Result.Success(new PublicProductModel(product.Id.Value, product.Name, product.Brand, product.ImageUrl,
            product.BaseUnit.ToString(), product.BaseAmount, product.CaloriesPerBase, product.ProteinsPerBase,
            product.FatsPerBase, product.CarbsPerBase, product.FiberPerBase, product.AlcoholPerBase) {
            Description = product.Description,
            Images = product.Images.Select(image => image.ImageUrl).ToArray(),
        });
    }
}
