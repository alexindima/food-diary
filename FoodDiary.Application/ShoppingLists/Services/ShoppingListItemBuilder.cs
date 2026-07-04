using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Services;

public static class ShoppingListItemBuilder {
    public static async Task<Result<IReadOnlyList<ShoppingListItemData>>> BuildItemsAsync(
        IReadOnlyList<ShoppingListItemInput> items,
        UserId userId,
        IProductLookupService productLookupService,
        CancellationToken cancellationToken) {
        if (items.Count == 0) {
            return Result.Success<IReadOnlyList<ShoppingListItemData>>([]);
        }

        var productIds = items
            .Where(item => item.ProductId.HasValue)
            .Select(item => item.ProductId!.Value)
            .ToList();

        if (items.Any(item => item.Id == Guid.Empty)) {
            return Result.Failure<IReadOnlyList<ShoppingListItemData>>(
                Errors.Validation.Invalid(nameof(ShoppingListItemInput.Id), "Id must not be empty."));
        }

        if (productIds.Any(id => id == Guid.Empty)) {
            return Result.Failure<IReadOnlyList<ShoppingListItemData>>(
                Errors.Validation.Invalid(nameof(ShoppingListItemInput.ProductId), "ProductId must not be empty."));
        }

        var normalizedProductIds = productIds
            .Select(id => new ProductId(id))
            .Distinct()
            .ToList();

        IReadOnlyDictionary<ProductId, Product> products = await productLookupService.GetAccessibleByIdsAsync(normalizedProductIds, userId, cancellationToken).ConfigureAwait(false);
        if (products.Count == normalizedProductIds.Count) {
            return BuildNormalizedItems(items, products);
        }

        ProductId missing = normalizedProductIds.First(id => !products.ContainsKey(id));
        return Result.Failure<IReadOnlyList<ShoppingListItemData>>(Errors.Product.NotAccessible(missing.Value));
    }

    private static Result<IReadOnlyList<ShoppingListItemData>> BuildNormalizedItems(
        IReadOnlyList<ShoppingListItemInput> items,
        IReadOnlyDictionary<ProductId, Product> products) {
        var normalized = new List<ShoppingListItemData>(items.Count);
        for (int index = 0; index < items.Count; index++) {
            ShoppingListItemInput item = items[index];
            Result<ShoppingListItemData> itemResult = BuildItem(item, index, products);
            if (itemResult.IsFailure) {
                return Result.Failure<IReadOnlyList<ShoppingListItemData>>(
                    itemResult.Error);
            }

            normalized.Add(itemResult.Value);
        }

        return Result.Success<IReadOnlyList<ShoppingListItemData>>(normalized);
    }

    private static Result<ShoppingListItemData> BuildItem(
        ShoppingListItemInput item,
        int index,
        IReadOnlyDictionary<ProductId, Product> products) {
        Error? amountError = ValidateAmount(item);
        if (amountError is not null) {
            return Result.Failure<ShoppingListItemData>(amountError);
        }

        return item.ProductId.HasValue
            ? Result.Success(BuildProductItem(item, index, products))
            : BuildCustomItem(item, index);
    }

    private static Error? ValidateAmount(ShoppingListItemInput item) {
        if (item.Amount.HasValue && (double.IsNaN(item.Amount.Value) || double.IsInfinity(item.Amount.Value))) {
            return Errors.Validation.Invalid(nameof(item.Amount), "Amount must be a finite number.");
        }

        return item.Amount <= 0
            ? Errors.Validation.Invalid(nameof(item.Amount), "Amount must be greater than zero.")
            : null;
    }

    private static ShoppingListItemData BuildProductItem(
        ShoppingListItemInput item,
        int index,
        IReadOnlyDictionary<ProductId, Product> products) {
        var productId = new ProductId(item.ProductId!.Value);
        Product product = products[productId];
        return new ShoppingListItemData(
            ToItemId(item.Id),
            product.Name,
            productId,
            item.Amount,
            product.BaseUnit,
            item.Category ?? product.Category,
            item.Aisle ?? item.Category ?? product.Category,
            item.Note,
            item.IsChecked,
            item.CheckedOnUtc,
            ResolveSortOrder(item.SortOrder, index));
    }

    private static Result<ShoppingListItemData> BuildCustomItem(ShoppingListItemInput item, int index) {
        if (string.IsNullOrWhiteSpace(item.Name)) {
            return Result.Failure<ShoppingListItemData>(Errors.Validation.Required(nameof(item.Name)));
        }

        Result<MeasurementUnit?> unitResult = ParseUnit(item.Unit);
        if (unitResult.IsFailure) {
            return Result.Failure<ShoppingListItemData>(unitResult.Error);
        }

        return Result.Success(new ShoppingListItemData(
            ToItemId(item.Id),
            item.Name.Trim(),
            ProductId: null,
            item.Amount,
            unitResult.Value,
            item.Category,
            item.Aisle ?? item.Category,
            item.Note,
            item.IsChecked,
            item.CheckedOnUtc,
            ResolveSortOrder(item.SortOrder, index)));
    }

    private static Result<MeasurementUnit?> ParseUnit(string? value) {
        return EnumValueParser.ParseOptional<MeasurementUnit>(
            value,
            nameof(ShoppingListItemInput.Unit),
            "Unknown measurement unit value.");
    }

    private static int ResolveSortOrder(int? sortOrder, int index) =>
        sortOrder is > 0 ? sortOrder.Value : index + 1;

    private static ShoppingListItemId? ToItemId(Guid? id) =>
        id.HasValue ? new ShoppingListItemId(id.Value) : null;
}
