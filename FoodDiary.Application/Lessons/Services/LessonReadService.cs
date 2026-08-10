using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Mappings;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Services;

public sealed class LessonReadService(
    INutritionLessonReadModelRepository readModelRepository)
    : ILessonReadService {
    public async Task<LessonPageModel> GetPageByLocaleAsync(
        UserId userId,
        string locale,
        LessonCategory? categoryFilter,
        LessonDifficulty? difficultyFilter,
        string? search,
        LessonSortOption sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken) {
        int skip = (page - 1) * pageSize;
        LessonSummaryPageReadModel result = await readModelRepository
            .GetSummaryPageByLocaleAsync(locale, categoryFilter, difficultyFilter, search, sort, skip, pageSize, cancellationToken)
            .ConfigureAwait(false);

        string effectiveLocale = locale;
        if (result.TotalLessonCount == 0 && !string.Equals(locale, "en", StringComparison.Ordinal)) {
            effectiveLocale = "en";
            result = await readModelRepository
                .GetSummaryPageByLocaleAsync("en", categoryFilter, difficultyFilter, search, sort, skip, pageSize, cancellationToken)
                .ConfigureAwait(false);
        }

        IReadOnlyList<Guid> readLessonIds = await readModelRepository.GetReadLessonIdsAsync(userId, cancellationToken).ConfigureAwait(false);
        var readIds = new HashSet<Guid>(readLessonIds);
        int readLessonCount = await readModelRepository.CountReadLessonsByLocaleAsync(userId, effectiveLocale, cancellationToken).ConfigureAwait(false);
        int totalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize);

        return new LessonPageModel(
            result.Items.Select(lesson => lesson.ToSummaryModel(readIds)).ToList(),
            page,
            pageSize,
            result.TotalCount,
            totalPages,
            result.TotalLessonCount,
            readLessonCount);
    }

    public async Task<LessonDetailModel?> GetByIdAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken) {
        LessonDetailReadModel? lesson = await readModelRepository.GetDetailReadModelByIdAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return null;
        }

        bool isRead = await readModelRepository
            .IsLessonReadAsync(userId, lessonId, cancellationToken)
            .ConfigureAwait(false);

        return lesson.ToDetailModel(isRead);
    }
}
