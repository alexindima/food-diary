using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Application.Lessons.Common;

namespace FoodDiary.Application.Lessons.Services;

public sealed class LessonAdministrationReadService(INutritionLessonReadModelRepository repository)
    : ILessonAdministrationReadService {
    public Task<IReadOnlyList<LessonAdminReadModel>> GetLessonsAsync(CancellationToken cancellationToken) =>
        repository.GetAdminReadModelsAsync(cancellationToken);
}
