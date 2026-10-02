using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Queries.ExportCatalogProducts;

public sealed class ExportCatalogProductsQueryHandler(IProductOverviewReadService reader)
    : IQueryHandler<ExportCatalogProductsQuery, Result<IReadOnlyList<CatalogProductModel>>> {
    private const int MaximumItems = 5000;

    public async Task<Result<IReadOnlyList<CatalogProductModel>>> Handle(ExportCatalogProductsQuery query, CancellationToken cancellationToken) {
        var result = new List<CatalogProductModel>();
        for (int page = 1; ; page++) {
            (IReadOnlyList<ProductOverviewReadItem> items, int total) = await reader.GetPagedAsync(
                UserId.Empty, includePublic: true, page, limit: 100, new ProductQueryFilters(Search: null), cancellationToken).ConfigureAwait(false);
            if (total > MaximumItems) {
                return Result.Failure<IReadOnlyList<CatalogProductModel>>(Errors.Validation.Invalid("Catalog", "Export supports at most 5000 products."));
            }
            result.AddRange(items.Select(item => new CatalogProductModel(
                item.Id.Value, item.Name, item.Barcode, item.Brand, item.ProductType.ToString(), item.Category,
                item.Description, item.ImageUrl, item.BaseUnit.ToString(), item.BaseAmount, item.DefaultPortionAmount,
                item.CaloriesPerBase, item.ProteinsPerBase, item.FatsPerBase, item.CarbsPerBase, item.FiberPerBase, item.AlcoholPerBase)));
            if (result.Count >= total || items.Count == 0) {
                return Result.Success<IReadOnlyList<CatalogProductModel>>(result);
            }
        }
    }
}
