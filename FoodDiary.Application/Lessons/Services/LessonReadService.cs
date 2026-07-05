using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Mappings;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Services;

public sealed class LessonReadService(INutritionLessonReadRepository repository)
    : ILessonReadService {
    public async Task<IReadOnlyList<LessonSummaryModel>> GetByLocaleAsync(
        UserId userId,
        string locale,
        LessonCategory? categoryFilter,
        CancellationToken cancellationToken) {
        IReadOnlyList<NutritionLesson> lessons = await repository
            .GetByLocaleAsync(locale, categoryFilter, cancellationToken)
            .ConfigureAwait(false);

        if (lessons.Count == 0 && !string.Equals(locale, "en", StringComparison.Ordinal)) {
            lessons = await repository.GetByLocaleAsync("en", categoryFilter, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<UserLessonProgress> progress = await repository.GetUserProgressAsync(userId, cancellationToken).ConfigureAwait(false);
        var readIds = new HashSet<NutritionLessonId>([.. progress.Select(static item => item.LessonId)]);

        return lessons
            .OrderBy(static lesson => lesson.Category)
            .ThenBy(static lesson => lesson.SortOrder)
            .Select(lesson => lesson.ToSummaryModel(readIds))
            .ToList();
    }

    public async Task<LessonDetailModel?> GetByIdAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken) {
        NutritionLesson? lesson = await repository.GetByIdAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return null;
        }

        UserLessonProgress? progress = await repository
            .GetUserProgressForLessonAsync(userId, lessonId, cancellationToken)
            .ConfigureAwait(false);

        return lesson.ToDetailModel(progress is not null);
    }
}
