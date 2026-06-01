using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Services;

public sealed record ShoppingListItemData(
    string Name,
    ProductId? ProductId,
    double? Amount,
    MeasurementUnit? Unit,
    string? Category,
    bool IsChecked,
    int SortOrder);
