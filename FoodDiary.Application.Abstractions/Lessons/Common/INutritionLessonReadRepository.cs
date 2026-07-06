using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Lessons.Common;

public interface INutritionLessonReadRepository {
    Task<IReadOnlyList<NutritionLesson>> GetByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<LessonSummaryReadModel>> GetSummaryReadModelsByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<NutritionLesson> lessons = await GetByLocaleAsync(locale, category, cancellationToken).ConfigureAwait(false);
        return [.. lessons.Select(ToSummaryReadModel)];
    }

    Task<IReadOnlyList<NutritionLesson>> GetAllAsync(
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<LessonAdminReadModel>> GetAdminReadModelsAsync(
        CancellationToken cancellationToken = default) {
        IReadOnlyList<NutritionLesson> lessons = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return [.. lessons.Select(ToAdminReadModel)];
    }

    Task<NutritionLesson?> GetByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default);

    async Task<LessonDetailReadModel?> GetDetailReadModelByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default) {
        NutritionLesson? lesson = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return lesson is null ? null : ToDetailReadModel(lesson);
    }

    Task<IReadOnlyList<UserLessonProgress>> GetUserProgressAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<Guid>> GetReadLessonIdsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<UserLessonProgress> progress = await GetUserProgressAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. progress.Select(static item => item.LessonId.Value)];
    }

    Task<UserLessonProgress?> GetUserProgressForLessonAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken = default);

    async Task<bool> IsLessonReadAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken = default) {
        UserLessonProgress? progress = await GetUserProgressForLessonAsync(userId, lessonId, cancellationToken).ConfigureAwait(false);
        return progress is not null;
    }

    private static LessonSummaryReadModel ToSummaryReadModel(NutritionLesson lesson) =>
        new(
            lesson.Id.Value,
            lesson.Title,
            lesson.Summary,
            lesson.Category.ToString(),
            lesson.Difficulty.ToString(),
            lesson.EstimatedReadMinutes,
            lesson.SortOrder);

    private static LessonDetailReadModel ToDetailReadModel(NutritionLesson lesson) =>
        new(
            lesson.Id.Value,
            lesson.Title,
            lesson.Content,
            lesson.Summary,
            lesson.Category.ToString(),
            lesson.Difficulty.ToString(),
            lesson.EstimatedReadMinutes);

    private static LessonAdminReadModel ToAdminReadModel(NutritionLesson lesson) =>
        new(
            lesson.Id.Value,
            lesson.Title,
            lesson.Content,
            lesson.Summary,
            lesson.Locale,
            lesson.Category.ToString(),
            lesson.Difficulty.ToString(),
            lesson.EstimatedReadMinutes,
            lesson.SortOrder,
            lesson.CreatedOnUtc,
            lesson.ModifiedOnUtc);
}
