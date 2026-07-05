using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Common;

public interface ILessonReadService {
    Task<IReadOnlyList<LessonSummaryModel>> GetByLocaleAsync(
        UserId userId,
        string locale,
        LessonCategory? categoryFilter,
        CancellationToken cancellationToken);

    Task<LessonDetailModel?> GetByIdAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken);
}
