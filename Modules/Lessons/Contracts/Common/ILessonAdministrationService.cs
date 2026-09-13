using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Lessons.Contracts.Common;

public interface ILessonAdministrationService {
    Task<Result<LessonAdminReadModel>> CreateAsync(
        string title,
        string content,
        string? summary,
        string locale,
        LessonCategory category,
        LessonDifficulty difficulty,
        int estimatedReadMinutes,
        int sortOrder,
        CancellationToken cancellationToken,
        bool isPublished = true);

    Task<Result<LessonAdminReadModel>> UpdateAsync(
        NutritionLessonId lessonId,
        string title,
        string content,
        string? summary,
        string locale,
        LessonCategory category,
        LessonDifficulty difficulty,
        int estimatedReadMinutes,
        int sortOrder,
        CancellationToken cancellationToken,
        bool isPublished = true);

    Task<Result> DeleteAsync(NutritionLessonId lessonId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LessonAdminReadModel>>> ImportAsync(
        IReadOnlyList<LessonAdministrationItem> items,
        CancellationToken cancellationToken);
}
