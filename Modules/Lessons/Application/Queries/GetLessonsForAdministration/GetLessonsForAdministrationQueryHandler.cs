using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Modules.Lessons.Contracts.Queries.GetLessonsForAdministration;
using FoodDiary.Mediator;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Lessons.Application.Queries.GetLessonsForAdministration;

public sealed class GetLessonsForAdministrationQueryHandler(INutritionLessonReadModelRepository repository) : IRequestHandler<GetLessonsForAdministrationQuery, IReadOnlyList<LessonAdminReadModel>> {
    public Task<IReadOnlyList<LessonAdminReadModel>> Handle(GetLessonsForAdministrationQuery request, CancellationToken cancellationToken) {
        return repository.GetAdminReadModelsAsync(
            PaginationPolicy.NormalizePage(request.Page),
            PaginationPolicy.NormalizePageSize(request.Limit),
            cancellationToken);
    }

}
