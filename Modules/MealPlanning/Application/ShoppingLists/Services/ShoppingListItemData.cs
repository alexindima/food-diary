using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Services;

public sealed record ShoppingListItemData(
    ShoppingListItemId? Id,
    string Name,
    ProductId? ProductId,
    double? Amount,
    MeasurementUnit? Unit,
    string? Category,
    string? Aisle,
    string? Note,
    bool IsChecked,
    DateTime? CheckedOnUtc,
    int SortOrder);
