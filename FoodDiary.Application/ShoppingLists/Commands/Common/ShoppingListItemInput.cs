using System;

namespace FoodDiary.Application.ShoppingLists.Commands.Common;

public record ShoppingListItemInput(
    Guid? ProductId,
    string? Name,
    double? Amount,
    string? Unit,
    string? Category,
    bool IsChecked,
    int? SortOrder);
