using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;

namespace FoodDiary.Modules.Meals.Application.Queries.ReadDistinctMealDates;

public sealed class ReadDistinctMealDatesQueryHandler(IMealActivityReadRepository repository) : IRequestHandler<ReadDistinctMealDatesQuery, IReadOnlyList<DateTime>> {
    public Task<IReadOnlyList<DateTime>> Handle(ReadDistinctMealDatesQuery request, CancellationToken cancellationToken) =>
        repository.GetDistinctMealDatesAsync(request.UserId, request.DateFrom, request.DateTo, cancellationToken);
}
