using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Lessons.Services;

public sealed class LessonAdministrationService(INutritionLessonRepository repository)
    : ILessonAdministrationService {
    public async Task<Result<NutritionLesson>> CreateAsync(
        string title,
        string content,
        string? summary,
        string locale,
        LessonCategory category,
        LessonDifficulty difficulty,
        int estimatedReadMinutes,
        int sortOrder,
        CancellationToken cancellationToken) {
        var lesson = NutritionLesson.Create(
            title,
            content,
            summary,
            locale,
            category,
            difficulty,
            estimatedReadMinutes,
            sortOrder);

        await repository.AddAsync(lesson, cancellationToken).ConfigureAwait(false);
        return Result.Success(lesson);
    }

    public async Task<Result<NutritionLesson>> UpdateAsync(
        NutritionLessonId lessonId,
        string title,
        string content,
        string? summary,
        string locale,
        LessonCategory category,
        LessonDifficulty difficulty,
        int estimatedReadMinutes,
        int sortOrder,
        CancellationToken cancellationToken) {
        NutritionLesson? lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure<NutritionLesson>(Errors.Lesson.NotFound(lessonId.Value));
        }

        lesson.Update(title, content, summary, locale, category, difficulty, estimatedReadMinutes, sortOrder);
        await repository.UpdateAsync(lesson, cancellationToken).ConfigureAwait(false);
        return Result.Success(lesson);
    }

    public async Task<Result> DeleteAsync(NutritionLessonId lessonId, CancellationToken cancellationToken) {
        NutritionLesson? lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure(Errors.Lesson.NotFound(lessonId.Value));
        }

        await repository.DeleteAsync(lesson, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<NutritionLesson>>> ImportAsync(
        IReadOnlyList<LessonAdministrationItem> items,
        CancellationToken cancellationToken) {
        var parsedLessons = new List<NutritionLesson>(items.Count);
        for (int index = 0; index < items.Count; index++) {
            LessonAdministrationItem item = items[index];
            try {
                parsedLessons.Add(NutritionLesson.Create(
                    item.Title,
                    item.Content,
                    item.Summary,
                    item.Locale,
                    item.Category,
                    item.Difficulty,
                    item.EstimatedReadMinutes,
                    item.SortOrder));
            } catch (ArgumentException exception) {
                return Result.Failure<IReadOnlyList<NutritionLesson>>(
                    Errors.Validation.Invalid($"lessons[{index.ToString(CultureInfo.InvariantCulture)}]", exception.Message));
            }
        }

        IReadOnlyList<NutritionLesson> existingLessons = await repository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);
        var lessonsByContent = new Dictionary<LessonContentIdentity, NutritionLesson>();
        foreach (NutritionLesson existingLesson in existingLessons) {
            lessonsByContent.TryAdd(ToContentIdentity(existingLesson), existingLesson);
        }

        var importedLessons = new List<NutritionLesson>(parsedLessons.Count);
        var newLessons = new List<NutritionLesson>(parsedLessons.Count);
        foreach (NutritionLesson parsedLesson in parsedLessons) {
            LessonContentIdentity identity = ToContentIdentity(parsedLesson);
            if (lessonsByContent.TryGetValue(identity, out NutritionLesson? existingLesson)) {
                importedLessons.Add(existingLesson);
                continue;
            }

            lessonsByContent.Add(identity, parsedLesson);
            newLessons.Add(parsedLesson);
            importedLessons.Add(parsedLesson);
        }

        if (newLessons.Count > 0) {
            await repository.AddRangeAsync(newLessons, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success<IReadOnlyList<NutritionLesson>>(importedLessons);
    }

    private static LessonContentIdentity ToContentIdentity(NutritionLesson lesson) =>
        new(
            lesson.Title,
            lesson.Content,
            lesson.Summary,
            lesson.Locale,
            lesson.Category,
            lesson.Difficulty,
            lesson.EstimatedReadMinutes,
            lesson.SortOrder);

    private sealed record LessonContentIdentity(
        string Title,
        string Content,
        string? Summary,
        string Locale,
        LessonCategory Category,
        LessonDifficulty Difficulty,
        int EstimatedReadMinutes,
        int SortOrder);
}
