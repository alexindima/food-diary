using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Meals.Service.Contracts.Models;

namespace FoodDiary.Modules.Meals.Service.Contracts.Queries.GetMeals;

public record GetMealsQuery(
    Guid? UserId,
    int Page,
    int Limit,
    DateTime? DateFrom,
    DateTime? DateTo,
    IReadOnlyCollection<string>? MealTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null,
    bool? HasAiSession = null) : IQuery<Result<PagedResponse<MealModel>>>, IUserRequest;
