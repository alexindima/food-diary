using FoodDiary.Mediator;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Queries.ReadTotalMealCount;

public sealed record ReadTotalMealCountQuery(UserId UserId) : IRequest<int>;
