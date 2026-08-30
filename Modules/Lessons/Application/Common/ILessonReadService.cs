using FoodDiary.Application.Lessons.Models;
using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Common;

public interface ILessonReadService {
    Task<LessonPageModel> GetPageByLocaleAsync(
        UserId userId,
        string locale,
        LessonCategory? categoryFilter,
        LessonDifficulty? difficultyFilter,
        string? search,
        LessonSortOption sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<LessonDetailModel?> GetByIdAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken);
}
