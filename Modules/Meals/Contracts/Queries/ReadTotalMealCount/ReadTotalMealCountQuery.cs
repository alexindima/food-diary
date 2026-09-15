using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Queries.ReadTotalMealCount;

public sealed record ReadTotalMealCountQuery(UserId UserId) : IRequest<int>;
