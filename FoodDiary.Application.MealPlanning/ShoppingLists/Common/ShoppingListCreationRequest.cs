using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Common;

public sealed record ShoppingListCreationRequest(
    UserId UserId,
    string Name,
    IReadOnlyList<ShoppingListCreationItem> Items);
