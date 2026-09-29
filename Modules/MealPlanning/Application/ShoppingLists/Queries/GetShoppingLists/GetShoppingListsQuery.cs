using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingLists;

public record GetShoppingListsQuery(
    Guid? UserId, int Page = 1, int? PageSize = null, string? Search = null) : IQuery<Result<IReadOnlyList<ShoppingListSummaryModel>>>, IUserRequest;
