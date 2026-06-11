using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Services;

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
