using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Common;

public sealed record ShoppingListCreationRequest(
    UserId UserId,
    string Name,
    IReadOnlyList<ShoppingListCreationItem> Items);
