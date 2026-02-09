using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.ShoppingLists;

public record CreateShoppingListRequest(
    string Name,
    IReadOnlyList<ShoppingListItemRequest>? Items = null);

public record UpdateShoppingListRequest(
    string? Name = null,
    IReadOnlyList<ShoppingListItemRequest>? Items = null);

public record ShoppingListItemRequest(
    Guid? ProductId,
    string? Name,
    double? Amount,
    string? Unit,
    string? Category,
    bool IsChecked = false,
    int? SortOrder = null);
