using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using System.Globalization;

namespace FoodDiary.Application.Admin.Commands.ImportAdminLessons;

public sealed class ImportAdminLessonsCommandHandler(INutritionLessonWriteRepository repository)
    : ICommandHandler<ImportAdminLessonsCommand, Result<AdminLessonsImportModel>> {
    public async Task<Result<AdminLessonsImportModel>> Handle(
        ImportAdminLessonsCommand command,
        CancellationToken cancellationToken) {
        var lessons = new List<NutritionLesson>(command.Lessons.Count);

        for (int index = 0; index < command.Lessons.Count; index++) {
            ImportAdminLessonItem item = command.Lessons[index];

            if (!Enum.TryParse(item.Category, ignoreCase: true, out LessonCategory category)) {
                return Result.Failure<AdminLessonsImportModel>(
                    Errors.Validation.Invalid(string.Create(CultureInfo.InvariantCulture, $"lessons[{index}].category"), "Invalid lesson category."));
            }

            if (!Enum.TryParse(item.Difficulty, ignoreCase: true, out LessonDifficulty difficulty)) {
                return Result.Failure<AdminLessonsImportModel>(
                    Errors.Validation.Invalid(string.Create(CultureInfo.InvariantCulture, $"lessons[{index}].difficulty"), "Invalid lesson difficulty."));
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
                    Errors.Validation.Invalid($"lessons[{index.ToString(CultureInfo.InvariantCulture)}]", exception.Message));
            }
        }

        await repository.AddRangeAsync(lessons, cancellationToken).ConfigureAwait(false);

        List<AdminLessonModel> models = lessons
            .ConvertAll(static lesson => new AdminLessonModel(
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
                lesson.ModifiedOnUtc));

        return Result.Success(new AdminLessonsImportModel(models.Count, models));
    }
}
