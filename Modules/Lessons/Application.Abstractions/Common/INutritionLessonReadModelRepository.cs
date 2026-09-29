using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Modules.Lessons.Application.Abstractions.Models;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Lessons.Application.Abstractions.Common;

public interface INutritionLessonReadModelRepository {
    Task<IReadOnlyList<LessonSummaryReadModel>> GetSummaryReadModelsByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<LessonSummaryPageReadModel> GetSummaryPageByLocaleAsync(
        string locale,
        LessonCategory? category,
        LessonDifficulty? difficulty,
        string? search,
        LessonSortOption sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountReadLessonsByLocaleAsync(
        UserId userId,
        string locale,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<LessonAdminReadModel>> GetAdminReadModelsAsync(
        int page,
        int limit,
        CancellationToken cancellationToken = default) =>
        (await GetAdminReadModelsAsync(cancellationToken).ConfigureAwait(false)).Skip((page - 1) * limit).Take(limit).ToList();

    Task<IReadOnlyList<LessonAdminReadModel>> GetAdminReadModelsAsync(
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<LessonDetailReadModel?> GetDetailReadModelByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetReadLessonIdsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsLessonReadAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken = default);
}
