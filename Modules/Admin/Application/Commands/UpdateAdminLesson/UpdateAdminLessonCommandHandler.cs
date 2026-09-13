using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Modules.Admin.Application.Internal.Validation;
using FoodDiary.Domain.Enums;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Admin.Application.Commands.UpdateAdminLesson;

public sealed class UpdateAdminLessonCommandHandler(ILessonAdministrationService lessonAdministrationService)
    : ICommandHandler<UpdateAdminLessonCommand, Result<AdminLessonModel>> {
    public async Task<Result<AdminLessonModel>> Handle(
        UpdateAdminLessonCommand command,
        CancellationToken cancellationToken) {
        Result<LessonCategory> categoryResult = AdminLessonValueParser.ParseCategory(command.Category, "category");
        if (categoryResult.IsFailure) {
            return Result.Failure<AdminLessonModel>(categoryResult.Error);
        }

        Result<LessonDifficulty> difficultyResult = AdminLessonValueParser.ParseDifficulty(command.Difficulty, "difficulty");
        if (difficultyResult.IsFailure) {
            return Result.Failure<AdminLessonModel>(difficultyResult.Error);
        }

        Result<NutritionLessonId> lessonIdResult = RequiredIdParser.Parse(
            command.Id,
            nameof(command.Id),
            "Lesson id must not be empty.",
            value => new NutritionLessonId(value));
        if (lessonIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<AdminLessonModel, NutritionLessonId>(lessonIdResult);
        }

        Result<LessonAdminReadModel> lessonResult = await lessonAdministrationService.UpdateAsync(
            lessonIdResult.Value,
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
