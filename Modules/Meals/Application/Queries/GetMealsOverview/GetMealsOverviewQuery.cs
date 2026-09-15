using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Application.Models;

namespace FoodDiary.Modules.Meals.Application.Queries.GetMealsOverview;

public sealed record GetMealsOverviewQuery(
    Guid? UserId,
    int Page,
    int Limit,
    DateTime? DateFrom,
    DateTime? DateTo,
    int FavoriteLimit = 10,
    IReadOnlyCollection<string>? MealTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null,
    bool? HasAiSession = null)
    : IQuery<Result<MealOverviewModel>>, IUserRequest;
