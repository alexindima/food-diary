using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealCount;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;

namespace FoodDiary.Modules.Meals.Application.Queries.ReadMealCount;

public sealed class ReadMealCountQueryHandler(IMealActivityReadRepository repository) : IRequestHandler<ReadMealCountQuery, int> {
    public Task<int> Handle(ReadMealCountQuery request, CancellationToken cancellationToken) =>
        repository.GetCountAsync(request.UserId, request.Filters, cancellationToken);
}
