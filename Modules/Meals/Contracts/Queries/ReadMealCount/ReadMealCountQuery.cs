using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Queries.ReadMealCount;

public sealed record ReadMealCountQuery(UserId UserId, MealQueryFilters Filters) : IRequest<int>;
