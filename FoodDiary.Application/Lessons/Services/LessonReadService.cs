using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Mappings;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Services;

public sealed class LessonReadService(
    INutritionLessonReadModelRepository readModelRepository,
    INutritionLessonReadRepository repository)
    : ILessonReadService {
    public async Task<IReadOnlyList<LessonSummaryModel>> GetByLocaleAsync(
        UserId userId,
        string locale,
        LessonCategory? categoryFilter,
        CancellationToken cancellationToken) {
        IReadOnlyList<LessonSummaryReadModel> lessons = await readModelRepository
            .GetSummaryReadModelsByLocaleAsync(locale, categoryFilter, cancellationToken)
            .ConfigureAwait(false);

        if (lessons.Count == 0 && !string.Equals(locale, "en", StringComparison.Ordinal)) {
            lessons = await readModelRepository.GetSummaryReadModelsByLocaleAsync("en", categoryFilter, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<Guid> readLessonIds = await repository.GetReadLessonIdsAsync(userId, cancellationToken).ConfigureAwait(false);
        var readIds = new HashSet<Guid>(readLessonIds);

        return lessons
            .OrderBy(static lesson => lesson.Category, StringComparer.Ordinal)
            .ThenBy(static lesson => lesson.SortOrder)
            .Select(lesson => lesson.ToSummaryModel(readIds))
            .ToList();
    }

    public async Task<LessonDetailModel?> GetByIdAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken) {
        LessonDetailReadModel? lesson = await readModelRepository.GetDetailReadModelByIdAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return null;
        }

        bool isRead = await repository
            .IsLessonReadAsync(userId, lessonId, cancellationToken)
            .ConfigureAwait(false);

        return lesson.ToDetailModel(isRead);
    }
}
