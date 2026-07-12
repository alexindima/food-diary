using System.Globalization;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Commands.ImportAdminLessons;

public sealed class ImportAdminLessonsCommandHandler(ILessonAdministrationService lessonAdministrationService)
    : ICommandHandler<ImportAdminLessonsCommand, Result<AdminLessonsImportModel>> {
    public async Task<Result<AdminLessonsImportModel>> Handle(
        ImportAdminLessonsCommand command,
        CancellationToken cancellationToken) {
        var lessons = new List<LessonAdministrationItem>(command.Lessons.Count);

        for (int index = 0; index < command.Lessons.Count; index++) {
            ImportAdminLessonItem item = command.Lessons[index];

            Result<LessonCategory> categoryResult = AdminLessonValueParser.ParseCategory(
                item.Category,
                string.Create(CultureInfo.InvariantCulture, $"lessons[{index}].category"));
            if (categoryResult.IsFailure) {
                return Result.Failure<AdminLessonsImportModel>(categoryResult.Error);
            }

            Result<LessonDifficulty> difficultyResult = AdminLessonValueParser.ParseDifficulty(
                item.Difficulty,
                string.Create(CultureInfo.InvariantCulture, $"lessons[{index}].difficulty"));
            if (difficultyResult.IsFailure) {
                return Result.Failure<AdminLessonsImportModel>(difficultyResult.Error);
            }

            lessons.Add(new LessonAdministrationItem(
                item.Title,
                item.Content,
                item.Summary,
                item.Locale,
                categoryResult.Value,
                difficultyResult.Value,
                item.EstimatedReadMinutes,
                item.SortOrder));
        }

        Result<IReadOnlyList<NutritionLesson>> importResult = await lessonAdministrationService
            .ImportAsync(lessons, cancellationToken)
            .ConfigureAwait(false);
        if (importResult.IsFailure) {
            return Result.Failure<AdminLessonsImportModel>(importResult.Error);
        }

        List<AdminLessonModel> models = [.. importResult.Value.Select(static lesson => lesson.ToAdminModel())];
        return Result.Success(new AdminLessonsImportModel(models.Count, models));
    }
}
