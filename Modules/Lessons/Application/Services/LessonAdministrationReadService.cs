using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;

namespace FoodDiary.Application.Lessons.Services;

public sealed class LessonAdministrationReadService(INutritionLessonReadModelRepository repository)
    : ILessonAdministrationReadService {
    public Task<IReadOnlyList<LessonAdminReadModel>> GetLessonsAsync(CancellationToken cancellationToken) =>
        repository.GetAdminReadModelsAsync(cancellationToken);
}
