using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Commands.ImportUsdaCatalog;

public sealed class UsdaCatalogProductImporter(
    IProductCatalogIdReadService catalog,
    IProductWriteRepository products,
    IProductMutationTransactionRunner transactionRunner) {
    public async Task<IReadOnlySet<int>> FindNewAsync(
        IReadOnlyList<UsdaCatalogProduct> items, CancellationToken cancellationToken) {
        List<Product> candidates = await PrepareAsync(items, cancellationToken);
        return candidates.Select(product => product.UsdaFdcId!.Value).ToHashSet();
    }

    public Task<IReadOnlySet<int>> ImportAsync(
        IReadOnlyList<UsdaCatalogProduct> items, CancellationToken cancellationToken) =>
        transactionRunner.ExecuteAsync<IReadOnlySet<int>>(async token => {
            List<Product> candidates = await PrepareAsync(items, token);
            foreach (Product candidate in candidates) {
                await products.AddAsync(candidate, token);
            }
            return candidates.Select(product => product.UsdaFdcId!.Value).ToHashSet();
        }, cancellationToken);

    private async Task<List<Product>> PrepareAsync(
        IReadOnlyList<UsdaCatalogProduct> items, CancellationToken cancellationToken) {
        var candidates = new List<Product>(items.Count);
        foreach (UsdaCatalogProduct item in items) {
            // Stable catalog identity for this provider, independent of the translation and target database.
            var id = new ProductId(Guid.Parse(FormattableString.Invariant($"67fb0bd8-09f9-88f5-80a9-{item.FdcId:x12}")));
            Product candidate = CreateProduct(item, id);
            bool? match = await catalog.FindUsdaCatalogMatchAsync(id.Value, item.FdcId, cancellationToken);
            if (match is false) {
                throw new InvalidOperationException(FormattableString.Invariant($"The catalog ID for USDA {item.FdcId} belongs to a different product."));
            }
            if (match is null) {
                candidates.Add(candidate);
            }
        }
        return candidates;
    }

    private static Product CreateProduct(UsdaCatalogProduct item, ProductId id) {
        Result<ProductType> type = ProductCommandParsers.ParseRequiredProductType(item.ProductType, nameof(item.ProductType));
        if (type.IsFailure || type.Value is ProductType.Unknown) {
            throw new InvalidOperationException(FormattableString.Invariant($"Invalid product type for USDA {item.FdcId}: {item.ProductType}"));
        }
        var product = Product.CreateWithMeasurements(SystemContentOwner.Id, item.Name,
            ProductMeasurementBasis.FromFields(MeasurementUnit.G, 100), defaultPortion: null,
            item.CaloriesPer100G, item.ProteinsPer100G, item.FatsPer100G, item.CarbsPer100G,
            item.FiberPer100G, item.AlcoholPer100G, productType: type.Value, category: item.Category,
            description: $"USDA FoodData Central (SR Legacy) · {item.SourceName}", visibility: Visibility.Public, importId: id);
        product.LinkToUsdaFood(item.FdcId);
        return product;
    }
}
