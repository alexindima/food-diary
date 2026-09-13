using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Modules.Admin.Application.Commands.CreateAdminLesson;

public sealed class CreateAdminLessonCommandHandler(ILessonAdministrationService lessonAdministrationService)
    : ICommandHandler<CreateAdminLessonCommand, Result<AdminLessonModel>> {
    public async Task<Result<AdminLessonModel>> Handle(
        CreateAdminLessonCommand command,
        CancellationToken cancellationToken) {
        Result<LessonCategory> categoryResult = AdminLessonValueParser.ParseCategory(command.Category, "category");
        if (categoryResult.IsFailure) {
            return Result.Failure<AdminLessonModel>(categoryResult.Error);
        }

        Result<LessonDifficulty> difficultyResult = AdminLessonValueParser.ParseDifficulty(command.Difficulty, "difficulty");
        if (difficultyResult.IsFailure) {
            return Result.Failure<AdminLessonModel>(difficultyResult.Error);
        }

        Result<LessonAdminReadModel> lessonResult = await lessonAdministrationService.CreateAsync(
            command.Title,
            command.Content,
            command.Summary,
            command.Locale,
            categoryResult.Value,
            difficultyResult.Value,
            command.EstimatedReadMinutes,
            command.SortOrder,
            cancellationToken, command.IsPublished).ConfigureAwait(false);

        return lessonResult.IsSuccess
            ? Result.Success(lessonResult.Value.ToAdminModel())
            : Result.Failure<AdminLessonModel>(lessonResult.Error);
    }
}
