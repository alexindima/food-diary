using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;

public sealed record ShoppingListCreationRequest(
    UserId UserId,
    string Name,
    IReadOnlyList<ShoppingListCreationItem> Items);
