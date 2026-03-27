using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.ShoppingLists.Commands.Common;
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
            .Select(item => new ProductId(item.ProductId!.Value))
            .Distinct()
            .ToList();

        var products = await productLookupService.GetAccessibleByIdsAsync(productIds, userId, cancellationToken);
        if (products.Count != productIds.Count) {
            var missing = productIds.First(id => !products.ContainsKey(id));
            return Result.Failure<IReadOnlyList<ShoppingListItemData>>(Errors.Product.NotAccessible(missing.Value));
        }

        var normalized = new List<ShoppingListItemData>(items.Count);
        for (var index = 0; index < items.Count; index++) {
            var item = items[index];
            if (item.Amount.HasValue && item.Amount.Value <= 0) {
                return Result.Failure<IReadOnlyList<ShoppingListItemData>>(
                    Errors.Validation.Invalid(nameof(item.Amount), "Amount must be greater than zero."));
            }

            if (item.ProductId.HasValue) {
                var productId = new ProductId(item.ProductId.Value);
                var product = products[productId];
                normalized.Add(new ShoppingListItemData(
                    product.Name,
                    productId,
                    item.Amount,
                    product.BaseUnit,
                    item.Category ?? product.Category,
                    item.IsChecked,
                    ResolveSortOrder(item.SortOrder, index)));
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Name)) {
                return Result.Failure<IReadOnlyList<ShoppingListItemData>>(
                    Errors.Validation.Required(nameof(item.Name)));
            }

            var unitResult = ParseUnit(item.Unit);
            if (unitResult.IsFailure) {
                return Result.Failure<IReadOnlyList<ShoppingListItemData>>(unitResult.Error);
            }

            normalized.Add(new ShoppingListItemData(
                item.Name.Trim(),
                null,
                item.Amount,
                unitResult.Value,
                item.Category,
                item.IsChecked,
                ResolveSortOrder(item.SortOrder, index)));
        }

        return Result.Success<IReadOnlyList<ShoppingListItemData>>(normalized);
    }

    private static Result<MeasurementUnit?> ParseUnit(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<MeasurementUnit?>(null);
        }

        return Enum.TryParse<MeasurementUnit>(value, true, out var parsed)
            ? Result.Success<MeasurementUnit?>(parsed)
            : Result.Failure<MeasurementUnit?>(
                Errors.Validation.Invalid(nameof(ShoppingListItemInput.Unit), "Unknown measurement unit value."));
    }

    private static int ResolveSortOrder(int? sortOrder, int index) =>
        sortOrder is > 0 ? sortOrder.Value : index + 1;
}

public sealed record ShoppingListItemData(
    string Name,
    ProductId? ProductId,
    double? Amount,
    MeasurementUnit? Unit,
    string? Category,
    bool IsChecked,
    int SortOrder);
