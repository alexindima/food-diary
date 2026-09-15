using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingLists;

public record GetShoppingListsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<ShoppingListSummaryModel>>>, IUserRequest;
