using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Modules.Lessons.Contracts.Queries.GetLessonsForAdministration;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Lessons.Application.Queries.GetLessonsForAdministration;

public sealed class GetLessonsForAdministrationQueryHandler(INutritionLessonReadModelRepository repository) : IRequestHandler<GetLessonsForAdministrationQuery, IReadOnlyList<LessonAdminReadModel>> {
    public Task<IReadOnlyList<LessonAdminReadModel>> Handle(GetLessonsForAdministrationQuery request, CancellationToken cancellationToken) {
        return repository.GetAdminReadModelsAsync(cancellationToken);
    }

}
