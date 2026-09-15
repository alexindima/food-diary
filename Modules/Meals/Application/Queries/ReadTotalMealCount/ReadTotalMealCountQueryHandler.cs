using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadTotalMealCount;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;

namespace FoodDiary.Modules.Meals.Application.Queries.ReadTotalMealCount;

public sealed class ReadTotalMealCountQueryHandler(IMealActivityReadRepository repository) : IRequestHandler<ReadTotalMealCountQuery, int> {
    public Task<int> Handle(ReadTotalMealCountQuery request, CancellationToken cancellationToken) =>
        repository.GetTotalMealCountAsync(request.UserId, cancellationToken);
}
