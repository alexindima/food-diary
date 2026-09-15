using FoodDiary.Mediator;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;

public sealed record ReadDistinctMealDatesQuery(UserId UserId, DateTime DateFrom, DateTime DateTo) : IRequest<IReadOnlyList<DateTime>>;
