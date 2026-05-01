using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Commands.ImportAdminLessons;

public sealed class ImportAdminLessonsCommandHandler(INutritionLessonRepository repository)
    : ICommandHandler<ImportAdminLessonsCommand, Result<AdminLessonsImportModel>> {
    public async Task<Result<AdminLessonsImportModel>> Handle(
        ImportAdminLessonsCommand command,
        CancellationToken cancellationToken) {
        var lessons = new List<NutritionLesson>(command.Lessons.Count);

        for (var index = 0; index < command.Lessons.Count; index++) {
            var item = command.Lessons[index];

            if (!Enum.TryParse<LessonCategory>(item.Category, true, out var category)) {
                return Result.Failure<AdminLessonsImportModel>(
                    Errors.Validation.Invalid($"lessons[{index}].category", "Invalid lesson category."));
            }

            if (!Enum.TryParse<LessonDifficulty>(item.Difficulty, true, out var difficulty)) {
                return Result.Failure<AdminLessonsImportModel>(
                    Errors.Validation.Invalid($"lessons[{index}].difficulty", "Invalid lesson difficulty."));
            }

            try {
                lessons.Add(NutritionLesson.Create(
                    item.Title,
                    item.Content,
                    item.Summary,
                    item.Locale,
                    category,
                    difficulty,
                    item.EstimatedReadMinutes,
                    item.SortOrder));
            } catch (ArgumentException exception) {
                return Result.Failure<AdminLessonsImportModel>(
                    Errors.Validation.Invalid($"lessons[{index}]", exception.Message));
            }
        }

        await repository.AddRangeAsync(lessons, cancellationToken);

        var models = lessons
            .Select(static lesson => new AdminLessonModel(
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
                lesson.ModifiedOnUtc))
            .ToList();

        return Result.Success(new AdminLessonsImportModel(models.Count, models));
    }
}
