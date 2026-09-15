using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealsForExport;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;

namespace FoodDiary.Modules.Meals.Application.Queries.ReadMealsForExport;

public sealed class ReadMealsForExportQueryHandler(IMealProjectionReadRepository repository) : IRequestHandler<ReadMealsForExportQuery, IReadOnlyList<MealProjectionReadModel>> {
    public Task<IReadOnlyList<MealProjectionReadModel>> Handle(ReadMealsForExportQuery request, CancellationToken cancellationToken) =>
        repository.GetByPeriodMealProjectionsAsync(request.UserId, request.DateFrom, request.DateTo, request.Limit, cancellationToken);
}
