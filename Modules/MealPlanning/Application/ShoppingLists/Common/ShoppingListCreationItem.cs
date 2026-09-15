using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;

public sealed record ShoppingListCreationItem(
    ProductId ProductId,
    string Name,
    double Amount,
    MeasurementUnit? Unit,
    string? Category,
    int SortOrder,
    IReadOnlyList<ShoppingListCreationSource> Sources);
