using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;

public record CreateShoppingListCommand(
    UserId? UserId,
    string Name,
    IReadOnlyList<ShoppingListItemInput> Items) : ICommand<Result<ShoppingListResponse>>;
