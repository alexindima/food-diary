using FoodDiary.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.ShoppingLists.Common;

public interface IShoppingListReadRepository {
    Task<ShoppingList?> GetByIdAsync(
        ShoppingListId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    async Task<ShoppingListReadModel?> GetReadModelByIdAsync(
        ShoppingListId id,
        UserId userId,
        CancellationToken cancellationToken = default) {
        ShoppingList? list = await GetByIdAsync(id, userId, includeItems: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        return list is null ? null : ToReadModel(list);
    }

    Task<ShoppingList?> GetCurrentAsync(
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    async Task<ShoppingListReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        ShoppingList? list = await GetCurrentAsync(userId, includeItems: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        return list is null ? null : ToReadModel(list);
    }

    Task<IReadOnlyList<ShoppingList>> GetAllAsync(
        UserId userId,
        bool includeItems = false,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<ShoppingList> lists = await GetAllAsync(userId, includeItems: true, cancellationToken).ConfigureAwait(false);
        return [.. lists.Select(static list => new ShoppingListSummaryReadModel(
            list.Id.Value,
            list.Name,
            list.CreatedOnUtc,
            list.Items.Count))];
    }

    private static ShoppingListReadModel ToReadModel(ShoppingList list) =>
        new(
            list.Id.Value,
            list.Name,
            list.CreatedOnUtc,
            [.. list.Items
                .OrderBy(static item => item.SortOrder)
                .ThenBy(static item => item.Id.Value)
                .Select(static item => new ShoppingListItemReadModel(
                    item.Id.Value,
                    item.ShoppingListId.Value,
                    item.ProductId?.Value,
                    item.Name,
                    item.Amount,
                    item.Unit?.ToString(),
                    item.Category,
                    item.Aisle,
                    item.Note,
                    item.IsChecked,
                    item.CheckedOnUtc,
                    item.SortOrder,
                    [.. item.Sources
                        .OrderBy(static source => source.DayNumber ?? int.MaxValue)
                        .ThenBy(static source => source.Label, StringComparer.Ordinal)
                        .Select(static source => new ShoppingListItemSourceReadModel(
                            source.Id.Value,
                            source.SourceType.ToString(),
                            source.MealPlanId?.Value,
                            source.MealPlanMealId?.Value,
                            source.RecipeId?.Value,
                            source.Label,
                            source.DayNumber,
                            source.MealType,
                            source.Amount,
                            source.Unit?.ToString()))]))]);
}
