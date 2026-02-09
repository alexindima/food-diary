using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.ShoppingLists;

public record ShoppingListResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyList<ShoppingListItemResponse> Items);

public record ShoppingListItemResponse(
    Guid Id,
    Guid ShoppingListId,
    Guid? ProductId,
    string Name,
    double? Amount,
    string? Unit,
    string? Category,
    bool IsChecked,
    int SortOrder);
