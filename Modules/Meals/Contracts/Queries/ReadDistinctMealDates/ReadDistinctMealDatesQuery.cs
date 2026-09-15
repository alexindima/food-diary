using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;

public sealed record ReadDistinctMealDatesQuery(UserId UserId, DateTime DateFrom, DateTime DateTo) : IRequest<IReadOnlyList<DateTime>>;
