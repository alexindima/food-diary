using System.Globalization;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Contracts.Commands.ImportLessons;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Lessons.Commands.ImportLessons;

public sealed class ImportLessonsCommandHandler(INutritionLessonReadRepository readRepository,
    INutritionLessonWriteRepository repository) : IRequestHandler<ImportLessonsCommand, Result<IReadOnlyList<LessonAdminReadModel>>> {
    public async Task<Result<IReadOnlyList<LessonAdminReadModel>>> Handle(ImportLessonsCommand request, CancellationToken cancellationToken) {
        IReadOnlyList<LessonAdministrationItem> items = request.Items;
        var parsedLessons = new List<NutritionLesson>(items.Count);
        for (int index = 0; index < items.Count; index++) {
            LessonAdministrationItem item = items[index];
            try {
                var parsed = NutritionLesson.Create(
                    item.Title,
                    item.Content,
                    item.Summary,
                    item.Locale,
                    item.Category,
                    item.Difficulty,
                    item.EstimatedReadMinutes,
                    item.SortOrder);
                parsed.SetPublication(item.IsPublished);
                parsedLessons.Add(parsed);
            } catch (ArgumentException exception) {
                return Result.Failure<IReadOnlyList<LessonAdminReadModel>>(
                    Errors.Validation.Invalid($"lessons[{index.ToString(CultureInfo.InvariantCulture)}]", exception.Message));
            }
        }

        IReadOnlyList<NutritionLesson> existingLessons = await readRepository
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

        return Result.Success<IReadOnlyList<LessonAdminReadModel>>(importedLessons.Select(ToAdminReadModel).ToArray());

    }

    private static LessonAdminReadModel ToAdminReadModel(NutritionLesson lesson) =>
        new(lesson.Id.Value, lesson.Title, lesson.Content, lesson.Summary, lesson.Locale,
            lesson.Category.ToString(), lesson.Difficulty.ToString(), lesson.EstimatedReadMinutes,
            lesson.SortOrder, lesson.CreatedOnUtc, lesson.ModifiedOnUtc, lesson.IsPublished);

    private static LessonContentIdentity ToContentIdentity(NutritionLesson lesson) =>
        new(
            lesson.Title,
            lesson.Content,
            lesson.Summary,
            lesson.Locale,
            lesson.Category,
            lesson.Difficulty,
            lesson.EstimatedReadMinutes,
            lesson.SortOrder, lesson.IsPublished);

    private sealed record LessonContentIdentity(
        string Title,
        string Content,
        string? Summary,
        string Locale,
        LessonCategory Category,
        LessonDifficulty Difficulty,
        int EstimatedReadMinutes,
        int SortOrder, bool IsPublished);

}
