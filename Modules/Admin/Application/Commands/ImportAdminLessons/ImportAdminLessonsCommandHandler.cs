using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Mediator;
using FoodDiary.Modules.Lessons.Contracts.Commands.ImportLessons;
using System.Globalization;
using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;

namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminLessons;

public sealed class ImportAdminLessonsCommandHandler(ISender lessonAdministrationService)
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
                item.SortOrder, item.IsPublished));
        }

        Result<IReadOnlyList<LessonAdminReadModel>> importResult = await lessonAdministrationService.Send(new ImportLessonsCommand(Items: lessons), cancellationToken)
            .ConfigureAwait(false);
        if (importResult.IsFailure) {
            return Result.Failure<AdminLessonsImportModel>(importResult.Error);
        }

        List<AdminLessonModel> models = [.. importResult.Value.Select(static lesson => lesson.ToAdminModel())];
        return Result.Success(new AdminLessonsImportModel(models.Count, models));
    }
}
