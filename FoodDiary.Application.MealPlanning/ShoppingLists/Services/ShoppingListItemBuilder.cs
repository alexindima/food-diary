using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.MealPlanning.Common.Validation;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Services;

public static class ShoppingListItemBuilder {
    public static async Task<Result<IReadOnlyList<ShoppingListItemData>>> BuildItemsAsync(
        IReadOnlyList<ShoppingListItemInput> items,
        UserId userId,
        IProductLookupService productLookupService,
        CancellationToken cancellationToken) {
        if (items.Count > ShoppingListInputLimits.ItemsMaxCount) {
            return Result.Failure<IReadOnlyList<ShoppingListItemData>>(
                Errors.Validation.Invalid(nameof(items), $"A shopping list must contain at most {ShoppingListInputLimits.ItemsMaxCount} items."));
        }

        if (items.Count == 0) {
            return Result.Success<IReadOnlyList<ShoppingListItemData>>([]);
        }

        if (items.Any(item => item.Id == Guid.Empty)) {
            return Result.Failure<IReadOnlyList<ShoppingListItemData>>(
                Errors.Validation.Invalid(nameof(ShoppingListItemInput.Id), "Id must not be empty."));
        }

        Result<IReadOnlyList<ProductId>> productIdsResult = ParseProductIds(items);
        if (productIdsResult.IsFailure) {
            return Result.Failure<IReadOnlyList<ShoppingListItemData>>(productIdsResult.Error);
        }

        IReadOnlyList<ProductId> productIds = productIdsResult.Value;

        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> products = await productLookupService.GetAccessibleByIdsAsync(productIds, userId, cancellationToken).ConfigureAwait(false);
        if (products.Count == productIds.Count) {
            return BuildNormalizedItems(items, products);
        }

        ProductId missing = productIds.First(id => !products.ContainsKey(id));
        return Result.Failure<IReadOnlyList<ShoppingListItemData>>(Errors.Product.NotAccessible(missing.Value));
    }

    private static Result<IReadOnlyList<ProductId>> ParseProductIds(IReadOnlyList<ShoppingListItemInput> items) {
        var productIds = new HashSet<ProductId>();
        foreach (ShoppingListItemInput item in items) {
            Result<ProductId?> productIdResult = OptionalEntityIdValidator.Parse(
                item.ProductId,
                nameof(ShoppingListItemInput.ProductId),
                "ProductId",
                value => new ProductId(value));
            if (productIdResult.IsFailure) {
                return Result.Failure<IReadOnlyList<ProductId>>(productIdResult.Error);
            }

            if (productIdResult.Value.HasValue) {
                productIds.Add(productIdResult.Value.Value);
            }
        }

        return Result.Success<IReadOnlyList<ProductId>>(productIds.ToList());
    }

    private static Result<IReadOnlyList<ShoppingListItemData>> BuildNormalizedItems(
        IReadOnlyList<ShoppingListItemInput> items,
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> products) {
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
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> products) {
        Error? textError = ValidateText(item);
        if (textError is not null) {
            return Result.Failure<ShoppingListItemData>(textError);
        }

        Error? amountError = ValidateAmount(item);
        if (amountError is not null) {
            return Result.Failure<ShoppingListItemData>(amountError);
        }

        return item.ProductId.HasValue
            ? BuildProductItem(item, index, products)
            : BuildCustomItem(item, index);
    }

    private static Error? ValidateAmount(ShoppingListItemInput item) {
        if (item.Amount.HasValue && (double.IsNaN(item.Amount.Value) || double.IsInfinity(item.Amount.Value))) {
            return Errors.Validation.Invalid(nameof(item.Amount), "Amount must be a finite number.");
        }

        return item.Amount is <= 0 or > ShoppingListInputLimits.AmountMaxValue
            ? Errors.Validation.Invalid(nameof(item.Amount), ShoppingListInputLimits.AmountRangeErrorMessage)
            : null;
    }

    private static Error? ValidateText(ShoppingListItemInput item) {
        if (!item.ProductId.HasValue && item.Name?.Trim().Length > ShoppingListInputLimits.ItemNameMaxLength) {
            return Errors.Validation.Invalid(nameof(item.Name), $"Name must be at most {ShoppingListInputLimits.ItemNameMaxLength} characters.");
        }

        if (item.Category?.Trim().Length > ShoppingListInputLimits.CategoryMaxLength) {
            return Errors.Validation.Invalid(nameof(item.Category), $"Category must be at most {ShoppingListInputLimits.CategoryMaxLength} characters.");
        }

        if (item.Aisle?.Trim().Length > ShoppingListInputLimits.CategoryMaxLength) {
            return Errors.Validation.Invalid(nameof(item.Aisle), $"Aisle must be at most {ShoppingListInputLimits.CategoryMaxLength} characters.");
        }

        return item.Note?.Trim().Length > ShoppingListInputLimits.NoteMaxLength
            ? Errors.Validation.Invalid(nameof(item.Note), $"Note must be at most {ShoppingListInputLimits.NoteMaxLength} characters.")
            : null;
    }

    private static Result<ShoppingListItemData> BuildProductItem(
        ShoppingListItemInput item,
        int index,
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> products) {
        Result<ProductId?> productIdResult = OptionalEntityIdValidator.Parse(
            item.ProductId,
            nameof(ShoppingListItemInput.ProductId),
            "ProductId",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return Result.Failure<ShoppingListItemData>(productIdResult.Error);
        }

        ProductId productId = productIdResult.Value!.Value;
        ProductOverviewReadItem product = products[productId];
        return Result.Success(new ShoppingListItemData(
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
            ResolveSortOrder(item.SortOrder, index)));
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
