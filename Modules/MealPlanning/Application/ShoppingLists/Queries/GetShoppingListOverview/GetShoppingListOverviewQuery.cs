using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingListOverview;

public sealed record GetShoppingListOverviewQuery(Guid? UserId) : IQuery<Result<ShoppingListOverviewModel>>, IUserRequest;
