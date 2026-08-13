using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Common;

public sealed record ShoppingListCreationItem(
    ProductId ProductId,
    string Name,
    double Amount,
    MeasurementUnit? Unit,
    string? Category,
    int SortOrder,
    IReadOnlyList<ShoppingListCreationSource> Sources);
