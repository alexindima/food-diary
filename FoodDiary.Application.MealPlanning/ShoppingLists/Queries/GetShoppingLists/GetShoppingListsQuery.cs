using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.ShoppingLists.Models;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetShoppingLists;

public record GetShoppingListsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<ShoppingListSummaryModel>>>, IUserRequest;
