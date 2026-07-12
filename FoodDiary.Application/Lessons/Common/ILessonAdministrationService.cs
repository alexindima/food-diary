using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Lessons.Common;

public interface ILessonAdministrationService {
    Task<Result<NutritionLesson>> CreateAsync(
        string title,
        string content,
        string? summary,
        string locale,
        LessonCategory category,
        LessonDifficulty difficulty,
        int estimatedReadMinutes,
        int sortOrder,
        CancellationToken cancellationToken);

    Task<Result<NutritionLesson>> UpdateAsync(
        NutritionLessonId lessonId,
        string title,
        string content,
        string? summary,
        string locale,
        LessonCategory category,
        LessonDifficulty difficulty,
        int estimatedReadMinutes,
        int sortOrder,
        CancellationToken cancellationToken);

    Task<Result> DeleteAsync(NutritionLessonId lessonId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<NutritionLesson>>> ImportAsync(
        IReadOnlyList<LessonAdministrationItem> items,
        CancellationToken cancellationToken);
}
