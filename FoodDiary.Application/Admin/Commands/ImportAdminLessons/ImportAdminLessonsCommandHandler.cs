using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
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

            try {
                lessons.Add(NutritionLesson.Create(
                    item.Title,
                    item.Content,
                    item.Summary,
                    item.Locale,
                    categoryResult.Value,
                    difficultyResult.Value,
                    item.EstimatedReadMinutes,
                    item.SortOrder));
            } catch (ArgumentException exception) {
                return Result.Failure<AdminLessonsImportModel>(
                    Errors.Validation.Invalid($"lessons[{index.ToString(CultureInfo.InvariantCulture)}]", exception.Message));
            }
        }

        await repository.AddRangeAsync(lessons, cancellationToken).ConfigureAwait(false);

        List<AdminLessonModel> models = lessons.ConvertAll(static lesson => lesson.ToAdminModel());

        return Result.Success(new AdminLessonsImportModel(models.Count, models));
    }
}
